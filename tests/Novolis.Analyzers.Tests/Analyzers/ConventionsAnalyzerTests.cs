using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
