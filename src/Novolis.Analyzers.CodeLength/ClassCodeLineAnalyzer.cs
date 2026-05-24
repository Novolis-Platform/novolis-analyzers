using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Novolis.Analyzers.CodeLength.Internals;

namespace Novolis.Analyzers.CodeLength;

/// <summary>
/// Reports a warning when a named type exceeds <see cref="CodeLengthSettings.ClassMaxLines"/> lines.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassCodeLineAnalyzer : DiagnosticAnalyzer
{
	private DiagnosticDescriptor Rule => new TooManyLinesInClassDescriptorProvider().GetDescriptor();
		
	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];
		
	/// <inheritdoc />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterSymbolAction(TypeSymbolAction, SymbolKind.NamedType);
	}

	private void TypeSymbolAction(SymbolAnalysisContext obj)
	{
		if (obj.Symbol is not INamedTypeSymbol typeSymbol)
			return;

		if (typeSymbol.DeclaringSyntaxReferences.Length == 0)
			return;

		var syntaxTree = typeSymbol.DeclaringSyntaxReferences[0].SyntaxTree;

		if (syntaxTree.TryGetText(out var resultText) && resultText.Lines.Count > CodeLengthSettings.ClassMaxLines)
		{
			obj.ReportDiagnostic(new DiagnosticBuilder().WithDescriptor(Rule).WithLocation(typeSymbol.Locations[0]).WithArguments(typeSymbol.Name, CodeLengthSettings.ClassMaxLines).Build());
		}
	}
}
