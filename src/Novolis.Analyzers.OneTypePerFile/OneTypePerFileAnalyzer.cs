using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.OneTypePerFile;

/// <summary>
/// Warns when a source file declares more than one top-level type (<c>NOV2201</c>).
/// Nested types stay with their parent. Partials of the same type count as one type.
/// The check is syntax-only so it stays cheap on large files.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneTypePerFileAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        OneTypePerFileFacts.DiagnosticId,
        "Declare one type per file",
        "'{0}' declares more than one type. Move '{1}' into its own file.",
        "Novolis.FileLayout",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        if (context.Tree.GetRoot(context.CancellationToken) is not CompilationUnitSyntax root)
            return;

        var types = OneTypePerFileFacts.GetTopLevelTypes(root);
        if (types.Count <= 1)
            return;

        var primary = OneTypePerFileFacts.SelectPrimaryName(context.Tree.FilePath, types);
        if (primary is null)
            return;

        var distinct = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            var name = OneTypePerFileFacts.GetTypeName(type);
            if (name.Length == 0 || !seen.Add(name))
                continue;

            distinct++;
        }

        if (distinct <= 1)
            return;

        var fileName = OneTypePerFileFacts.FileDisplayName(context.Tree.FilePath);
        foreach (var type in types)
        {
            var name = OneTypePerFileFacts.GetTypeName(type);
            if (name.Length == 0 || string.Equals(name, primary, StringComparison.Ordinal))
                continue;

            var properties = ImmutableDictionary<string, string?>.Empty.Add(OneTypePerFileFacts.TypeNameProperty, name);
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                OneTypePerFileFacts.GetIdentifierLocation(type),
                properties,
                fileName,
                name));
        }
    }
}
