using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Novolis.Analyzers.OneTypePerFile;

/// <summary>Fix All for <c>NOV2201</c> at document, project, and solution scope.</summary>
internal sealed class OneTypePerFileFixAllProvider : FixAllProvider
{
    internal static readonly OneTypePerFileFixAllProvider Instance = new();

    private OneTypePerFileFixAllProvider()
    {
    }

    /// <inheritdoc />
    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
    [
        FixAllScope.Document,
        FixAllScope.Project,
        FixAllScope.Solution,
    ];

    /// <inheritdoc />
    public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) =>
        Task.FromResult<CodeAction?>(CodeAction.Create(
            "Move extra types into their own files",
            ct => ApplyAsync(fixAllContext, ct),
            OneTypePerFileFacts.EquivalenceKey));

    private static async Task<Solution> ApplyAsync(FixAllContext fixAllContext, CancellationToken cancellationToken)
    {
        var solution = fixAllContext.Solution;
        var byDocument = new Dictionary<DocumentId, HashSet<string>>();

        if (fixAllContext.Scope == FixAllScope.Document)
        {
            if (fixAllContext.Document is not null)
                await CollectAsync(fixAllContext, solution, byDocument, await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false)).ConfigureAwait(false);
        }
        else if (fixAllContext.Scope == FixAllScope.Project)
        {
            await CollectAsync(fixAllContext, solution, byDocument, await fixAllContext.GetProjectDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false)).ConfigureAwait(false);
        }
        else if (fixAllContext.Scope == FixAllScope.Solution)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (project is null || project.Language != LanguageNames.CSharp)
                    continue;

                var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                await CollectAsync(fixAllContext, solution, byDocument, diagnostics).ConfigureAwait(false);
            }
        }

        return await OneTypePerFileEdit.MoveExtraTypesAsync(
            solution,
            byDocument.Keys.ToList(),
            byDocument,
            cancellationToken).ConfigureAwait(false);
    }

    private static Task CollectAsync(
        FixAllContext fixAllContext,
        Solution solution,
        Dictionary<DocumentId, HashSet<string>> byDocument,
        IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Location.SourceTree is not { } tree)
                continue;

            var document = solution.GetDocument(tree);
            if (document is null)
                continue;

            if (!fixAllContext.DiagnosticIds.Contains(diagnostic.Id))
                continue;

            if (!diagnostic.Properties.TryGetValue(OneTypePerFileFacts.TypeNameProperty, out var typeName)
                || typeName is null
                || typeName.Length == 0)
            {
                continue;
            }

            if (!byDocument.TryGetValue(document.Id, out var names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                byDocument.Add(document.Id, names);
            }

            names.Add(typeName);
        }

        return Task.CompletedTask;
    }
}
