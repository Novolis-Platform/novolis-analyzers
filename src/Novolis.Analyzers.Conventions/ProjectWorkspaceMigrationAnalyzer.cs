using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.Conventions;

/// <summary>Reports use of the compatibility project-workspace type name.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProjectWorkspaceMigrationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "NOV2103",
        "Use IProjectWorkspace for structured authoring",
        "'Novolis.Workspaces.IWorkspace' is a compatibility type. Use 'IProjectWorkspace'; reserve 'Novolis.IO.Workspace.IWorkspace' for a typed directory root.",
        "Novolis.Conventions",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is not INamedTypeSymbol namedType
            || !string.Equals(namedType.ToDisplayString(), "Novolis.Workspaces.IWorkspace", StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
    }
}
