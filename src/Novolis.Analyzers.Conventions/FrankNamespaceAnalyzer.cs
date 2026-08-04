using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.Conventions;

/// <summary>
/// Reports leftover <c>Frank.*</c> namespaces and usings in Novolis production assemblies (<c>NOV2102</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FrankNamespaceAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        "NOV2102",
        "No leftover Frank.* namespaces",
        "'{0}' uses the legacy Frank prefix; rename to Novolis.*",
        "Novolis.Conventions",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
    {
        if (!IsNovolisProductionAssembly(context.Compilation.AssemblyName))
            return;

        string? name = context.Node switch
        {
            NamespaceDeclarationSyntax ns => ns.Name.ToString(),
            FileScopedNamespaceDeclarationSyntax fs => fs.Name.ToString(),
            _ => null,
        };

        if (name is null || !IsFrankNamespace(name))
            return;

        var location = context.Node switch
        {
            NamespaceDeclarationSyntax ns => ns.Name.GetLocation(),
            FileScopedNamespaceDeclarationSyntax fs => fs.Name.GetLocation(),
            _ => context.Node.GetLocation(),
        };

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, name));
    }

    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        if (!IsNovolisProductionAssembly(context.Compilation.AssemblyName))
            return;

        if (context.Node is not UsingDirectiveSyntax usingDirective)
            return;

        if (usingDirective.Name is null)
            return;

        var name = usingDirective.Name.ToString();
        if (!IsFrankNamespace(name))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.Name.GetLocation(), name));
    }

    internal static bool IsFrankNamespace(string name) =>
        name.Equals("Frank", StringComparison.Ordinal)
        || name.StartsWith("Frank.", StringComparison.Ordinal);

    internal static bool IsNovolisProductionAssembly(string? assemblyName)
    {
        if (assemblyName is not { Length: > 0 } name)
            return false;

        if (!name.StartsWith("Novolis.", StringComparison.Ordinal)
            && !name.Equals("Novolis", StringComparison.Ordinal))
        {
            return false;
        }

        return !name.EndsWith(".Unit", StringComparison.Ordinal)
            && !name.EndsWith(".Tests", StringComparison.Ordinal)
            && !name.Contains(".Unit.", StringComparison.Ordinal)
            && !name.Contains(".Tests.", StringComparison.Ordinal);
    }
}
