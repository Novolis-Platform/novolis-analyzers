using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Novolis.Analyzers.ReadabilityComplexity;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

public sealed class ReadabilityComplexityAnalyzerTests
{
    [Test]
    public async Task ExampleA_StraightforwardOrchestration_ScoresZero()
    {
        const string code = """
                            public class Processor
                            {
                                private readonly ILogger logger;
                                private readonly IValidator validator;
                                private readonly IRepository repository;
                                private readonly IPublisher publisher;

                                public async System.Threading.Tasks.Task ProcessAsync(Document document, System.Threading.CancellationToken ct)
                                {
                                    logger.LogInformation("Processing document");
                                    await validator.ValidateAsync(document, ct);
                                    await repository.SaveAsync(document, ct);
                                    await publisher.PublishAsync(document, ct);
                                }
                            }

                            public interface ILogger { void LogInformation(string message); }
                            public interface IValidator { System.Threading.Tasks.Task ValidateAsync(Document document, System.Threading.CancellationToken ct); }
                            public interface IRepository { System.Threading.Tasks.Task SaveAsync(Document document, System.Threading.CancellationToken ct); }
                            public interface IPublisher { System.Threading.Tasks.Task PublishAsync(Document document, System.Threading.CancellationToken ct); }
                            public class Document { }
                            """;

        var score = await ScoreMethodAsync(code, "ProcessAsync");
        await Assert.That(score.Total).IsEqualTo(0);
    }

    [Test]
    public async Task ExampleB_ReadingAndDeciding_ScoresFour()
    {
        const string code = """
                            public class Processor
                            {
                                private readonly IRepository repository;
                                private readonly IProcessor processor;

                                public async System.Threading.Tasks.Task ProcessAsync(System.Guid id, System.Threading.CancellationToken ct)
                                {
                                    var document = await repository.GetAsync(id, ct);

                                    if (document is null)
                                        return;

                                    if (document.IsLocked)
                                        return;

                                    await processor.ProcessAsync(document, ct);
                                }
                            }

                            public interface IRepository { System.Threading.Tasks.Task<Document?> GetAsync(System.Guid id, System.Threading.CancellationToken ct); }
                            public interface IProcessor { System.Threading.Tasks.Task ProcessAsync(Document document, System.Threading.CancellationToken ct); }
                            public class Document { public bool IsLocked { get; set; } }
                            """;

        var score = await ScoreMethodAsync(code, "ProcessAsync");
        await Assert.That(score.Total).IsEqualTo(4);
        await Assert.That(score.Declarations).IsEqualTo(1);
        await Assert.That(score.ConsumedResults).IsEqualTo(1);
        await Assert.That(score.Decisions).IsEqualTo(2);
    }

    [Test]
    public async Task ExampleC_ExplicitMutation_ScoresTen()
    {
        const string code = """
                            public class Updater
                            {
                                public void Update(Request request)
                                {
                                    request.Status = Status.Processed;
                                }
                            }

                            public class Request { public Status Status { get; set; } }
                            public enum Status { Pending, Processed }
                            """;

        var score = await ScoreMethodAsync(code, "Update");
        await Assert.That(score.Total).IsEqualTo(10);
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(10);
    }

    [Test]
    public async Task ExampleD_DeclarativeRouting_ScoresOne()
    {
        const string code = """
                            public class Handler
                            {
                                public System.Threading.Tasks.Task HandleAsync(Document document, System.Threading.CancellationToken ct)
                                    => document.Kind switch
                                    {
                                        Kind.Invoice => HandleInvoiceAsync(document, ct),
                                        Kind.Credit => HandleCreditAsync(document, ct),
                                        Kind.Reminder => HandleReminderAsync(document, ct),
                                        _ => throw new System.NotSupportedException()
                                    };

                                private System.Threading.Tasks.Task HandleInvoiceAsync(Document document, System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask;
                                private System.Threading.Tasks.Task HandleCreditAsync(Document document, System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask;
                                private System.Threading.Tasks.Task HandleReminderAsync(Document document, System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask;
                            }

                            public class Document { public Kind Kind { get; set; } }
                            public enum Kind { Invoice, Credit, Reminder }
                            """;

        var score = await ScoreMethodAsync(code, "HandleAsync");
        await Assert.That(score.Total).IsEqualTo(1);
        await Assert.That(score.SwitchPoints).IsEqualTo(1);
        await Assert.That(score.ThrowPoints).IsEqualTo(0);
    }

    [Test]
    public async Task SimpleDispatchSwitchStatement_ScoresOne()
    {
        const string code = """
                            public class Handler
                            {
                                public async System.Threading.Tasks.Task HandleAsync(Document document)
                                {
                                    switch (document.Kind)
                                    {
                                        case Kind.Invoice:
                                            await HandleInvoiceAsync(document);
                                            break;
                                        case Kind.Credit:
                                            await HandleCreditAsync(document);
                                            break;
                                        case Kind.Reminder:
                                            await HandleReminderAsync(document);
                                            break;
                                    }
                                }

                                private System.Threading.Tasks.Task HandleInvoiceAsync(Document document) => System.Threading.Tasks.Task.CompletedTask;
                                private System.Threading.Tasks.Task HandleCreditAsync(Document document) => System.Threading.Tasks.Task.CompletedTask;
                                private System.Threading.Tasks.Task HandleReminderAsync(Document document) => System.Threading.Tasks.Task.CompletedTask;
                            }

                            public class Document { public Kind Kind { get; set; } }
                            public enum Kind { Invoice, Credit, Reminder }
                            """;

        var score = await ScoreMethodAsync(code, "HandleAsync");
        await Assert.That(score.Total).IsEqualTo(1);
        await Assert.That(score.SwitchPoints).IsEqualTo(1);
    }

    [Test]
    public async Task LocalObjectInitialization_DoesNotChargeExternalMutation()
    {
        const string code = """
                            public class Mapper
                            {
                                public Invoice Create()
                                {
                                    var result = new Invoice();
                                    result.Amount = 100;
                                    result.Currency = "NOK";
                                    return result;
                                }
                            }

                            public class Invoice
                            {
                                public int Amount { get; set; }
                                public string Currency { get; set; }
                            }
                            """;

        var score = await ScoreMethodAsync(code, "Create");
        await Assert.That(score.Declarations).IsEqualTo(1);
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(0);
        await Assert.That(score.Total).IsEqualTo(1);
    }

    [Test]
    public async Task RepeatedExternalMutations_ChargeFirstTenThenOne()
    {
        const string code = """
                            public class Mapper
                            {
                                public void Populate(Request request, string name, string address)
                                {
                                    request.Name = name;
                                    request.Address = address;
                                }
                            }

                            public class Request
                            {
                                public string Name { get; set; }
                                public string Address { get; set; }
                            }
                            """;

        var score = await ScoreMethodAsync(code, "Populate");
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(11);
        await Assert.That(score.Total).IsEqualTo(11);
    }

    [Test]
    public async Task FluentLinqAssignment_CountsOneConsumedResult()
    {
        const string code = """
                            using System.Collections.Generic;
                            using System.Linq;

                            public class Calc
                            {
                                public Dictionary<string, int> Run(IEnumerable<Invoice> invoices)
                                {
                                    var totals = invoices
                                        .Where(x => x.IsValid)
                                        .GroupBy(x => x.Currency)
                                        .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount));
                                    return totals;
                                }
                            }

                            public class Invoice
                            {
                                public bool IsValid { get; set; }
                                public string Currency { get; set; }
                                public int Amount { get; set; }
                            }
                            """;

        var score = await ScoreMethodAsync(code, "Run");
        await Assert.That(score.Declarations).IsEqualTo(1);
        await Assert.That(score.ConsumedResults).IsEqualTo(1);
        await Assert.That(score.Total).IsEqualTo(2);
    }

    [Test]
    public async Task Analyzer_ReportsWhenAboveDefaultThreshold()
    {
        // Three distinct external targets → 30 points (default threshold 20).
        const string code = """
                            public class Sample
                            {
                                public void Mutate(Request a, Request b, Request c)
                                {
                                    a.Status = 1;
                                    b.Status = 1;
                                    c.Status = 1;
                                }
                            }

                            public class Request { public int Status { get; set; } }
                            """;

        var diagnostics = await AnalyzeAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2301")).IsTrue();
    }

    [Test]
    public async Task Analyzer_DoesNotReportBelowDefaultThreshold()
    {
        // Single external mutation → 10 points (default threshold 20).
        const string code = """
                            public class Sample
                            {
                                public void Mutate(Request request)
                                {
                                    request.Status = 1;
                                }
                            }

                            public class Request { public int Status { get; set; } }
                            """;

        var diagnostics = await AnalyzeAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2301")).IsFalse();
    }

    [Test]
    public async Task FormatPrimaryCauses_ListsContributors()
    {
        var score = new ReadabilityScore(
            Declarations: 2,
            LocalReassignments: 0,
            Decisions: 1,
            SwitchPoints: 0,
            IterationPoints: 0,
            CatchPoints: 0,
            ThrowPoints: 0,
            ExternalMutationPoints: 10,
            ConsumedResults: 0);

        await Assert.That(score.FormatPrimaryCauses()).IsEqualTo("2 local declarations, 1 decision, and 10 external mutation cost");
        await Assert.That(score.FormatBreakdown().Contains("total=13")).IsTrue();
    }

    [Test]
    public async Task ConstructorDependencyInjection_IsFree()
    {
        const string code = """
                            public class Processor
                            {
                                private readonly IRepository _repository;
                                private readonly IPublisher publisher;

                                public Processor(IRepository repository, IPublisher publisher)
                                {
                                    _repository = repository;
                                    this.publisher = publisher;
                                }
                            }

                            public interface IRepository { }
                            public interface IPublisher { }
                            """;

        var score = await ScoreConstructorAsync(code);
        await Assert.That(score.Total).IsEqualTo(0);
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(0);
    }

    [Test]
    public async Task ConstructorUiChromeWiring_IsCappedCheap()
    {
        const string code = """
                            public class MainWindow
                            {
                                public string Title { get; set; }
                                public object Content { get; set; }
                                public int Width { get; set; }
                                public int Height { get; set; }
                                public bool CanResize { get; set; }
                                public bool ShowInTaskbar { get; set; }
                                public string Name { get; set; }
                                public object DataContext { get; set; }

                                public MainWindow()
                                {
                                    Title = "App";
                                    Content = null;
                                    Width = 800;
                                    Height = 600;
                                    CanResize = true;
                                    ShowInTaskbar = true;
                                    Name = "Main";
                                    DataContext = null;
                                }
                            }
                            """;

        var score = await ScoreConstructorAsync(code);
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(5);
        await Assert.That(score.Total).IsEqualTo(5);
    }

    [Test]
    public async Task BuildLayout_TreatsInstanceWiringAsCheap()
    {
        const string code = """
                            public class Shell
                            {
                                public object Root { get; set; }
                                public object Nav { get; set; }
                                public object Body { get; set; }
                                public object Status { get; set; }
                                public object Footer { get; set; }
                                public object Banner { get; set; }

                                public void BuildLayout()
                                {
                                    Root = new object();
                                    Nav = new object();
                                    Body = new object();
                                    Status = new object();
                                    Footer = new object();
                                    Banner = new object();
                                }
                            }
                            """;

        var score = await ScoreMethodAsync(code, "BuildLayout");
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(5);
        await Assert.That(score.Total).IsEqualTo(5);
    }

    [Test]
    public async Task ArgumentMutationOutsideConstructor_StillExpensive()
    {
        const string code = """
                            public class Updater
                            {
                                public void Update(Request request)
                                {
                                    request.Status = Status.Processed;
                                    request.Name = "x";
                                }
                            }

                            public class Request
                            {
                                public Status Status { get; set; }
                                public string Name { get; set; }
                            }
                            public enum Status { Pending, Processed }
                            """;

        var score = await ScoreMethodAsync(code, "Update");
        await Assert.That(score.ExternalMutationPoints).IsEqualTo(11);
    }

    [Test]
    public async Task InitLikeName_DetectsCompositionHelpers()
    {
        await Assert.That(ScoringContext.IsInitLikeName("ConfigureServices")).IsTrue();
        await Assert.That(ScoringContext.IsInitLikeName("BuildLayout")).IsTrue();
        await Assert.That(ScoringContext.IsInitLikeName("BuildAzureSetupPanel")).IsTrue();
        await Assert.That(ScoringContext.IsInitLikeName("InitializeComponent")).IsTrue();
        await Assert.That(ScoringContext.IsInitLikeName("Build")).IsFalse();
        await Assert.That(ScoringContext.IsInitLikeName("Initialize")).IsFalse();
        await Assert.That(ScoringContext.IsInitLikeName("ProcessAsync")).IsFalse();
        await Assert.That(ScoringContext.IsInitLikeName("RefreshListen")).IsFalse();
    }

    private static async Task<ReadabilityScore> ScoreMethodAsync(string code, string methodName)
    {
        var (compilation, tree) = CreateCompilation(code);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == methodName);

        SyntaxNode body = method.Body is not null
            ? method.Body
            : method.ExpressionBody!.Expression;

        return ReadabilityScorer.Score(body, model, CancellationToken.None);
    }

    private static async Task<ReadabilityScore> ScoreConstructorAsync(string code)
    {
        var (compilation, tree) = CreateCompilation(code);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();
        var ctor = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
        SyntaxNode body = ctor.Body is not null
            ? ctor.Body
            : ctor.ExpressionBody!.Expression;
        return ReadabilityScorer.Score(body, model, CancellationToken.None);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string code)
    {
        var (compilation, _) = CreateCompilation(code);
        var analyzer = new ReadabilityComplexityAnalyzer();
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }

    private static (CSharpCompilation Compilation, SyntaxTree Tree) CreateCompilation(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
        };

        TryAddReference(references, "System.Runtime");
        TryAddReference(references, "System.Collections");
        TryAddReference(references, "System.Linq");
        TryAddReference(references, "System.Threading");
        TryAddReference(references, "System.Threading.Tasks");
        TryAddReference(references, "netstandard");

        var compilation = CSharpCompilation.Create(
            "ReadabilityComplexityTest",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return (compilation, tree);
    }

    private static void TryAddReference(List<MetadataReference> references, string simpleName)
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.Ordinal));
            if (assembly is null || assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                return;

            if (references.Any(r => string.Equals(r.Display, assembly.Location, StringComparison.OrdinalIgnoreCase)))
                return;

            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
        catch
        {
            // best-effort for TFM facade assemblies
        }
    }
}
