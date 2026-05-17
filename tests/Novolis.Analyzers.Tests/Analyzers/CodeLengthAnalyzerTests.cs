using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Novolis.Analyzers.CodeLength;

namespace Novolis.Analyzers.Tests.Analyzers;

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

            diagnostics.Should().Contain(d => d.Id == "FRANK4010");
        }
        finally
        {
            CodeLengthSettings.MethodMaxLines = previousMax;
        }
    }
}
