using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Novolis.Analyzers.OneTypePerFile;

/// <summary>
/// Moves one extra type into its own file. Fix All covers the document, project, and solution.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OneTypePerFileCodeFixProvider)), Shared]
public sealed class OneTypePerFileCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(OneTypePerFileAnalyzer.Rule.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => OneTypePerFileFixAllProvider.Instance;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        if (!diagnostic.Properties.TryGetValue(OneTypePerFileFacts.TypeNameProperty, out var typeName)
            || typeName is null
            || typeName.Length == 0)
        {
            return;
        }

        var name = typeName;
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Move '" + name + "' to " + name + ".cs",
                createChangedSolution: ct => MoveOneAsync(context.Document, name, ct),
                equivalenceKey: OneTypePerFileFacts.EquivalenceKey),
            diagnostic);
    }

    private static Task<Solution> MoveOneAsync(Document document, string typeName, CancellationToken cancellationToken)
    {
        var only = new Dictionary<DocumentId, HashSet<string>>
        {
            [document.Id] = new HashSet<string>(StringComparer.Ordinal) { typeName },
        };

        return OneTypePerFileEdit.MoveExtraTypesAsync(
            document.Project.Solution,
            [document.Id],
            only,
            cancellationToken);
    }
}
