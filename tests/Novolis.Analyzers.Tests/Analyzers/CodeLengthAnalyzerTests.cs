using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Novolis.Analyzers.CodeLength;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

[NotInParallel]
public class CodeLengthAnalyzerTests
{
    [Test]
    public async Task MethodCodeLineAnalyzer_ReportsWhenMethodExceedsMaxLines()
    {
        var previousMax = CodeLengthSettings.MethodMaxLines;
        try
        {
            CodeLengthSettings.MethodMaxLines = 1;

            const string code = """
                                public class Sample
                                {
                                    public void LongMethod()
                                    {
                                        var a = 1;
                                        var b = 2;
                                    }
                                }
                                """;

            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("CodeLengthTest")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            var analyzer = new MethodCodeLineAnalyzer();
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
                .GetAnalyzerDiagnosticsAsync();

            await Assert.That(diagnostics.Any(d => d.Id == "FRANK4010")).IsTrue();
        }
        finally
        {
            CodeLengthSettings.MethodMaxLines = previousMax;
        }
    }

    [Test]
    public async Task ClassCodeLineAnalyzer_ReportsWhenClassExceedsMaxLines()
    {
        var previousMax = CodeLengthSettings.ClassMaxLines;
        try
        {
            CodeLengthSettings.ClassMaxLines = 1;

            const string code = """
                                public class Sample
                                {
                                    public void Method()
                                    {
                                        var a = 1;
                                    }
                                }
                                """;

            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("CodeLengthTest")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            var analyzer = new ClassCodeLineAnalyzer();
            var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
                .GetAnalyzerDiagnosticsAsync();

            await Assert.That(diagnostics.Any(d => d.Id == "FRANK4011")).IsTrue();
        }
        finally
        {
            CodeLengthSettings.ClassMaxLines = previousMax;
        }
    }
}
