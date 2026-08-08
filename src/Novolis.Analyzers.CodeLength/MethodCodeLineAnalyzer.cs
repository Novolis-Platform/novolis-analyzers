using System.Collections.Immutable;
using Novolis.Analyzers.CodeLength.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.CodeLength;

/// <summary>
/// Reports a warning when a method exceeds <see cref="CodeLengthSettings.MethodMaxLines"/> statement lines.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodCodeLineAnalyzer : DiagnosticAnalyzer
{
    private DiagnosticDescriptor Rule => new TooManyLinesInMethodDescriptorProvider().GetDescriptor();
		
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];
		
    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(MethodSymbolAction, SymbolKind.Method);
    }

    private void MethodSymbolAction(SymbolAnalysisContext obj)
    {
        var methodSymbol = (IMethodSymbol)obj.Symbol;
        var syntaxTree = methodSymbol.DeclaringSyntaxReferences[0].SyntaxTree;

        if (syntaxTree.TryGetText(out var resultText) && resultText.Lines.Count(line => line.Text!.ToString().Contains($";")) > CodeLengthSettings.MethodMaxLines)
        {
            obj.ReportDiagnostic(new DiagnosticBuilder().WithDescriptor(Rule).WithLocation(methodSymbol.Locations[0]).WithArguments(methodSymbol.Name, CodeLengthSettings.MethodMaxLines).Build());
        }
    }
}
