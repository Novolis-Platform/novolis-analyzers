using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Novolis.Analyzers.Conventions;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

public sealed class ConventionsAnalyzerTests
{
    [Test]
    public async Task IdentifierWithDeskSegment_ReportsNov2101()
    {
        const string code = """
                            namespace Novolis.Game.Calypso;

                            public sealed class CaptainDeskModel
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Calypso", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsTrue();
    }

    [Test]
    public async Task DesktopIdentifier_DoesNotReportNov2101()
    {
        const string code = """
                            namespace Novolis.Avalonia.Shell;

                            public sealed class DesktopHost
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Avalonia.Shell", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsFalse();
    }

    [Test]
    public async Task StringLiteralWithDesk_ReportsNov2101()
    {
        const string code = """
                            namespace Novolis.Game.Calypso;

                            public static class Labels
                            {
                                public const string Title = "Captain desk";
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Calypso", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsTrue();
    }

    [Test]
    public async Task StringLiteralDesktop_DoesNotReportNov2101()
    {
        const string code = """
                            namespace Novolis.Avalonia.Shell;

                            public static class Labels
                            {
                                public const string Title = "Desktop PC";
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Avalonia.Shell", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsFalse();
    }

    [Test]
    public async Task FrankNamespace_ReportsNov2102()
    {
        const string code = """
                            namespace Frank.Analyzers.CodeLength;

                            public static class X
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Analyzers.Conventions", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsTrue();
    }

    [Test]
    public async Task FrankUsing_ReportsNov2102()
    {
        const string code = """
                            using Frank.Core;

                            namespace Novolis.Game.Identity;

                            public static class Id
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Identity", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsTrue();
    }

    [Test]
    public async Task NovolisNamespace_DoesNotReportNov2102()
    {
        const string code = """
                            namespace Novolis.Game.Identity;

                            public static class Id
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Identity", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsFalse();
    }

    [Test]
    public async Task FrankNamespace_InTestAssembly_DoesNotReportNov2102()
    {
        const string code = """
                            namespace Frank.TestHelpers;

                            public static class X
                            {
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Identity.Tests", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsFalse();
    }

    [Test]
    public async Task FrankNamespaceCodeFix_IsRegisteredForNov2102()
    {
        var fixer = new FrankNamespaceCodeFixProvider();
        await Assert.That(fixer.FixableDiagnosticIds.Contains("NOV2102")).IsTrue();
        await Assert.That(FrankNamespaceAnalyzer.IsFrankNamespace("Frank.Analyzers.CodeLength")).IsTrue();
        await Assert.That("Novolis" + "Frank.Analyzers.CodeLength".Substring("Frank".Length))
            .IsEqualTo("Novolis.Analyzers.CodeLength");
    }

    [Test]
    public async Task SplitIdentifierSegments_DesktopIsSingleSegment()
    {
        var segments = DeskWordAnalyzer.SplitIdentifierSegments("DesktopHost").ToArray();
        await Assert.That(segments.Length).IsEqualTo(2);
        await Assert.That(segments[0]).IsEqualTo("Desktop");
        await Assert.That(segments[1]).IsEqualTo("Host");
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("DesktopHost")).IsFalse();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("CaptainDeskModel")).IsTrue();
    }

    [Test]
    public async Task CommentWithDesk_ReportsNov2101()
    {
        const string code = """
                            namespace Novolis.Game.Calypso;

                            // Captain desk layout
                            public sealed class Bridge
                            {
                                /* desk panel */
                                /// <summary>desk chrome</summary>
                                public void Open() { }
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Calypso", code);
        await Assert.That(diagnostics.Count(d => d.Id == "NOV2101")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task CommentDesktop_DoesNotReportNov2101()
    {
        const string code = """
                            namespace Novolis.Avalonia.Shell;

                            // Desktop layout
                            public sealed class Host { }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Avalonia.Shell", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsFalse();
    }

    [Test]
    public async Task MethodPropertyEventParameter_WithDesk_ReportsNov2101()
    {
        const string code = """
                            namespace Novolis.Game.Calypso;

                            public sealed class Panel
                            {
                                public string DeskTitle { get; set; }
                                public event System.Action DeskOpened;
                                public void OpenDesk() { }
                                public void Bind(string deskId) { }
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Calypso", code);
        await Assert.That(diagnostics.Count(d => d.Id == "NOV2101")).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task BlockFrankNamespace_ReportsNov2102()
    {
        const string code = """
                            namespace Frank.Legacy
                            {
                                public static class X { }
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Analyzers.Conventions", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsTrue();
    }

    [Test]
    public async Task UnderscoreAndDigitDeskSegments_ReportNov2101()
    {
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("desk_host")).IsTrue();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("desk-host")).IsTrue();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("Desk2Model")).IsTrue();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("_2desk")).IsTrue();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("XMLDeskHost")).IsTrue();
        await Assert.That(DeskWordAnalyzer.IdentifierContainsDeskSegment("")).IsFalse();
        var acronym = DeskWordAnalyzer.SplitIdentifierSegments("XMLHOST").ToArray();
        await Assert.That(acronym.Length).IsEqualTo(1);
        await Assert.That(acronym[0]).IsEqualTo("XMLHOST");
        await Assert.That(DeskWordAnalyzer.TextContainsDeskWord("open the desk now")).IsTrue();
        await Assert.That(DeskWordAnalyzer.TextContainsDeskWord("Desktop")).IsFalse();
        await Assert.That(DeskWordAnalyzer.TextContainsDeskWord("")).IsFalse();
    }

    [Test]
    public async Task FieldWithDesk_ReportsNov2101()
    {
        const string code = """
                            namespace Novolis.Game.Calypso;

                            public sealed class Panel
                            {
                                public int deskCount;
                            }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Calypso", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2101")).IsTrue();
    }

    [Test]
    public async Task FrankUsing_InTestAssembly_DoesNotReportNov2102()
    {
        const string code = """
                            using Frank.Core;

                            namespace Novolis.Game.Identity;

                            public static class Id { }
                            """;

        var diagnostics = await AnalyzeAsync("Novolis.Game.Identity.Tests", code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2102")).IsFalse();
    }

    [Test]
    public async Task NonFrankUsing_And_NonNovolisAssembly_DoNotReportNov2102()
    {
        const string systemUsing = """
                                   using System;

                                   namespace Novolis.Game.Identity;

                                   public static class Id { }
                                   """;
        var novolisDiagnostics = await AnalyzeAsync("Novolis.Game.Identity", systemUsing);
        await Assert.That(novolisDiagnostics.Any(d => d.Id == "NOV2102")).IsFalse();

        const string frankNs = """
                               namespace Frank.Legacy;

                               public static class X { }
                               """;
        var contosoDiagnostics = await AnalyzeAsync("Contoso.Lib", frankNs);
        await Assert.That(contosoDiagnostics.Any(d => d.Id == "NOV2102")).IsFalse();

        await Assert.That(FrankNamespaceAnalyzer.IsNovolisProductionAssembly(null)).IsFalse();
        await Assert.That(FrankNamespaceAnalyzer.IsNovolisProductionAssembly("")).IsFalse();
        await Assert.That(FrankNamespaceAnalyzer.IsNovolisProductionAssembly("Contoso.Lib")).IsFalse();
    }

    [Test]
    public async Task ExactFrankNamespace_And_UnitAssembly_Filter()
    {
        const string frankOnly = """
                                 namespace Frank;

                                 public static class X { }
                                 """;
        var frankDiagnostics = await AnalyzeAsync("Novolis.Game.Identity", frankOnly);
        await Assert.That(frankDiagnostics.Any(d => d.Id == "NOV2102")).IsTrue();

        const string unitAsm = """
                               namespace Frank.Helpers;

                               public static class X { }
                               """;
        var unitDiagnostics = await AnalyzeAsync("Novolis.Game.Identity.Unit.Helpers", unitAsm);
        await Assert.That(unitDiagnostics.Any(d => d.Id == "NOV2102")).IsFalse();

        await Assert.That(FrankNamespaceAnalyzer.IsFrankNamespace("Frank")).IsTrue();
        await Assert.That(FrankNamespaceAnalyzer.IsFrankNamespace("Novolis")).IsFalse();
    }

    [Test]
    public async Task FrankNamespaceCodeFix_AppliesExactFrankRename()
    {
        const string code = """
                            namespace Frank;

                            public static class X { }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "Novolis.Analyzers.Conventions",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new FrankNamespaceAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.Single(d => d.Id == "NOV2102");

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "Novolis.Analyzers.Conventions",
            "Novolis.Analyzers.Conventions",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
        var document = workspace.AddDocument(project.Id, "File.cs", SourceText.From(code));

        var fixer = new FrankNamespaceCodeFixProvider();
        CodeAction? action = null;
        var context = new CodeFixContext(
            document,
            diagnostic,
            (a, _) => action ??= a,
            CancellationToken.None);
        await fixer.RegisterCodeFixesAsync(context);
        await Assert.That(action).IsNotNull();

        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        var apply = operations.OfType<ApplyChangesOperation>().Single();
        apply.Apply(workspace, CancellationToken.None);
        var fixedDoc = workspace.CurrentSolution.GetDocument(document.Id)!;
        var fixedText = (await fixedDoc.GetTextAsync()).ToString();
        await Assert.That(fixedText).Contains("namespace Novolis;");
        await Assert.That(fixedText.Contains("namespace Frank;", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task FrankNamespaceCodeFix_AppliesRename()
    {
        const string code = """
                            namespace Frank.Legacy;

                            public static class X { }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "Novolis.Analyzers.Conventions",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new FrankNamespaceAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.Single(d => d.Id == "NOV2102");

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "Novolis.Analyzers.Conventions",
            "Novolis.Analyzers.Conventions",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
        var document = workspace.AddDocument(project.Id, "File.cs", SourceText.From(code));

        var fixer = new FrankNamespaceCodeFixProvider();
        await Assert.That(fixer.GetFixAllProvider()).IsNotNull();

        CodeAction? action = null;
        var context = new CodeFixContext(
            document,
            diagnostic,
            (a, _) => action ??= a,
            CancellationToken.None);
        await fixer.RegisterCodeFixesAsync(context);
        await Assert.That(action).IsNotNull();

        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        var apply = operations.OfType<ApplyChangesOperation>().Single();
        apply.Apply(workspace, CancellationToken.None);
        var fixedDoc = workspace.CurrentSolution.GetDocument(document.Id)!;
        var fixedText = (await fixedDoc.GetTextAsync()).ToString();
        await Assert.That(fixedText).Contains("namespace Novolis.Legacy");
        await Assert.That(fixedText.Contains("Frank.Legacy", StringComparison.Ordinal)).IsFalse();
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string assemblyName, string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new DeskWordAnalyzer(),
            new FrankNamespaceAnalyzer());

        return await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
    }
}
