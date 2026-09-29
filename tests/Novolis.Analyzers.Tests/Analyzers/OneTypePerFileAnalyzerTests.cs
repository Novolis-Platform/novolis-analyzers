using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Novolis.Analyzers.OneTypePerFile;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

public sealed class OneTypePerFileAnalyzerTests
{
    private const string DirectoryPath = @"d:\novolis\novolis-analyzers\tests\_split";

    [Test]
    public async Task SingleType_DoesNotReport()
    {
        const string code = """
                            namespace Novolis.Sample;

                            public sealed class Keeper
                            {
                                public sealed class Nested { }
                            }
                            """;

        var diagnostics = await AnalyzeAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2201")).IsFalse();
    }

    [Test]
    public async Task TwoPartialsOfOneType_DoNotReport()
    {
        const string code = """
                            namespace Novolis.Sample;

                            public partial class Keeper { }

                            public partial class Keeper { }
                            """;

        var diagnostics = await AnalyzeAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2201")).IsFalse();
    }

    [Test]
    public async Task SecondType_ReportsWarningOnThatType()
    {
        const string code = """
                            namespace Novolis.Sample;

                            public sealed class Keeper { }

                            public delegate void Extra(int value);

                            public enum Other { A }
                            """;

        var diagnostics = await AnalyzeAsync(code, Path.Combine(DirectoryPath, "Keeper.cs"));
        var reported = diagnostics.Where(d => d.Id == "NOV2201").ToArray();
        await Assert.That(reported.Length).IsEqualTo(2);
        await Assert.That(reported.All(d => d.Severity == DiagnosticSeverity.Warning)).IsTrue();
        await Assert.That(reported.Select(d => d.GetMessage()).Any(m => m.Contains("Extra"))).IsTrue();
        await Assert.That(reported.Select(d => d.GetMessage()).Any(m => m.Contains("Other"))).IsTrue();
        await Assert.That(reported.Select(d => d.GetMessage()).Any(m => m.Contains("'Keeper'"))).IsFalse();
    }

    [Test]
    public async Task FileNameMatch_KeepsThatTypeAndReportsTheOther()
    {
        const string code = """
                            namespace Novolis.Sample;

                            public sealed class Keeper { }

                            public record Extra(int Value);
                            """;

        var diagnostics = await AnalyzeAsync(code, Path.Combine(DirectoryPath, "Extra.cs"));
        var reported = diagnostics.Where(d => d.Id == "NOV2201").ToArray();
        await Assert.That(reported.Length).IsEqualTo(1);
        await Assert.That(reported[0].GetMessage().Contains("'Keeper'")).IsTrue();
    }

    [Test]
    public async Task CodeFix_MovesExtraTypeAndLeavesFileMetadata()
    {
        const string code = """
                            global using System.IO;

                            using System;

                            [assembly: System.CLSCompliant(false)]

                            namespace Novolis.Sample;

                            public sealed class Keeper
                            {
                            }

                            public sealed class Extra
                            {
                                public int Value { get; set; }
                            }
                            """;

        var workspace = new AdhocWorkspace();
        var document = AddDocument(workspace, "Sample", "Keeper.cs", code);
        var diagnostic = (await AnalyzeAsync(code, Path.Combine(DirectoryPath, "Keeper.cs")))
            .Single(d => d.Id == "NOV2201");

        var fixer = new OneTypePerFileCodeFixProvider();
        var scopes = fixer.GetFixAllProvider()!.GetSupportedFixAllScopes().ToArray();
        await Assert.That(scopes).Contains(FixAllScope.Document);
        await Assert.That(scopes).Contains(FixAllScope.Project);
        await Assert.That(scopes).Contains(FixAllScope.Solution);

        CodeAction? action = null;
        var context = new CodeFixContext(document, diagnostic, (candidate, _) => action ??= candidate, CancellationToken.None);
        await fixer.RegisterCodeFixesAsync(context);
        await Assert.That(action).IsNotNull();
        await Assert.That(action!.Title).IsEqualTo("Move 'Extra' to Extra.cs");

        var operations = await action.GetOperationsAsync(CancellationToken.None);
        operations.OfType<ApplyChangesOperation>().Single().Apply(workspace, CancellationToken.None);

        var docs = workspace.CurrentSolution.Projects.Single().Documents.ToArray();
        await Assert.That(docs.Length).IsEqualTo(2);

        var keeper = docs.Single(d => d.Name == "Keeper.cs");
        var extra = docs.Single(d => d.Name == "Extra.cs");
        var keeperText = (await keeper.GetTextAsync()).ToString();
        var extraText = (await extra.GetTextAsync()).ToString();

        await Assert.That(keeperText).Contains("class Keeper");
        await Assert.That(keeperText.Contains("class Extra")).IsFalse();
        await Assert.That(keeperText).Contains("global using System.IO");
        await Assert.That(keeperText).Contains("assembly:");

        await Assert.That(extraText).Contains("class Extra");
        await Assert.That(extraText).Contains("namespace Novolis.Sample;");
        await Assert.That(extraText).Contains("using System;");
        await Assert.That(extraText.Contains("class Keeper")).IsFalse();
        await Assert.That(extraText.Contains("global using")).IsFalse();
        await Assert.That(extraText.Contains("assembly:")).IsFalse();
    }

    [Test]
    public async Task CodeFix_FileNameMatchMovesTheOtherType()
    {
        const string code = """
                            namespace Novolis.Sample
                            {
                                public sealed class Keeper { }
                            }

                            namespace Novolis.Sample.Other
                            {
                                public sealed class Extra { }
                            }
                            """;

        var workspace = new AdhocWorkspace();
        var document = AddDocument(workspace, "Sample", "Extra.cs", code);
        var diagnostic = (await AnalyzeAsync(code, Path.Combine(DirectoryPath, "Extra.cs")))
            .Single(d => d.Id == "NOV2201");

        CodeAction? action = null;
        var context = new CodeFixContext(
            document,
            diagnostic,
            (candidate, _) => action ??= candidate,
            CancellationToken.None);
        await new OneTypePerFileCodeFixProvider().RegisterCodeFixesAsync(context);
        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        operations.OfType<ApplyChangesOperation>().Single().Apply(workspace, CancellationToken.None);

        var docs = workspace.CurrentSolution.Projects.Single().Documents.ToArray();
        var extra = (await docs.Single(d => d.Name == "Extra.cs").GetTextAsync()).ToString();
        var keeper = (await docs.Single(d => d.Name == "Keeper.cs").GetTextAsync()).ToString();
        await Assert.That(extra).Contains("class Extra");
        await Assert.That(extra).Contains("namespace Novolis.Sample.Other");
        await Assert.That(extra.Contains("class Keeper")).IsFalse();
        await Assert.That(keeper).Contains("class Keeper");
        await Assert.That(keeper).Contains("namespace Novolis.Sample");
        await Assert.That(keeper.Contains("class Extra")).IsFalse();
        await Assert.That(keeper.Contains("Sample.Other")).IsFalse();
    }

    [Test]
    public async Task CodeFix_UsesNumericSuffixWhenFileNameIsTaken()
    {
        const string occupied = """
                                namespace Novolis.Sample;

                                public sealed class Other { }
                                """;
        const string code = """
                            namespace Novolis.Sample;

                            public sealed class Keeper { }

                            public sealed class Extra { }
                            """;

        var workspace = new AdhocWorkspace();
        var project = AddProject(workspace, "Sample");
        AddDocument(workspace, project, "Extra.cs", occupied);
        var document = AddDocument(workspace, project, "Keeper.cs", code);
        var diagnostic = (await AnalyzeAsync(code, Path.Combine(DirectoryPath, "Keeper.cs")))
            .Single(d => d.Id == "NOV2201");

        CodeAction? action = null;
        var context = new CodeFixContext(document, diagnostic, (candidate, _) => action ??= candidate, CancellationToken.None);
        await new OneTypePerFileCodeFixProvider().RegisterCodeFixesAsync(context);
        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        operations.OfType<ApplyChangesOperation>().Single().Apply(workspace, CancellationToken.None);

        var names = workspace.CurrentSolution.Projects.Single().Documents.Select(d => d.Name).ToArray();
        await Assert.That(names).Contains("Extra.1.cs");
        await Assert.That(names).Contains("Extra.cs");
    }

    [Test]
    public async Task Move_ProjectScopeLeavesOtherProjectUntouched_SolutionScopeSplitsBoth()
    {
        const string code = """
                            namespace Novolis.Sample;

                            public sealed class Keeper { }

                            public sealed class Extra { }
                            """;

        var workspace = new AdhocWorkspace();
        var left = AddDocument(workspace, "Left", "Keeper.cs", code);
        var right = AddDocument(workspace, "Right", "Keeper.cs", code);

        var projectOnly = await OneTypePerFileEdit.MoveExtraTypesAsync(
            workspace.CurrentSolution,
            [left.Id],
            onlyTypeNames: null,
            CancellationToken.None);

        var leftDocs = projectOnly.GetProject(left.Project.Id)!.Documents.Select(d => d.Name).ToArray();
        var rightDocs = projectOnly.GetProject(right.Project.Id)!.Documents.ToArray();
        await Assert.That(leftDocs).Contains("Extra.cs");
        await Assert.That(rightDocs.Length).IsEqualTo(1);
        await Assert.That((await rightDocs[0].GetTextAsync()).ToString()).Contains("class Extra");

        var solutionWide = await OneTypePerFileEdit.MoveExtraTypesAsync(
            workspace.CurrentSolution,
            [left.Id, right.Id],
            onlyTypeNames: null,
            CancellationToken.None);
        await Assert.That(solutionWide.Projects.SelectMany(p => p.Documents).Count()).IsEqualTo(4);
        foreach (var document in solutionWide.Projects.SelectMany(p => p.Documents))
        {
            var text = (await document.GetTextAsync()).ToString();
            var declaresKeeper = text.Contains("class Keeper");
            var declaresExtra = text.Contains("class Extra");
            await Assert.That(declaresKeeper && declaresExtra).IsFalse();
        }
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string code, string? path = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code, path: path ?? string.Empty);
        var compilation = CSharpCompilation.Create(
            "Sample",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new OneTypePerFileAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
    }

    private static Document AddDocument(AdhocWorkspace workspace, string projectName, string fileName, string code)
    {
        var project = AddProject(workspace, projectName);
        return AddDocument(workspace, project, fileName, code);
    }

    private static Project AddProject(AdhocWorkspace workspace, string projectName)
    {
        return workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            projectName,
            projectName,
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
    }

    private static Document AddDocument(AdhocWorkspace workspace, Project project, string fileName, string code)
    {
        var documentId = DocumentId.CreateNewId(project.Id);
        var info = DocumentInfo.Create(
            documentId,
            fileName,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code), VersionStamp.Create())),
            filePath: Path.Combine(DirectoryPath, fileName));
        if (!workspace.TryApplyChanges(workspace.CurrentSolution.AddDocument(info)))
            throw new InvalidOperationException("Could not add " + fileName);

        return workspace.CurrentSolution.GetDocument(documentId)
            ?? throw new InvalidOperationException("Missing " + fileName);
    }
}
