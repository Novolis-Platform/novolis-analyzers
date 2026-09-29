using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Novolis.Analyzers.OneTypePerFile;

/// <summary>
/// Moves non-primary top-level types into new documents. Callers choose the document set,
/// which is how a single file, a project, or a solution is fixed.
/// </summary>
internal static class OneTypePerFileEdit
{
    internal static async Task<Solution> MoveExtraTypesAsync(
        Solution solution,
        IReadOnlyCollection<DocumentId> documentIds,
        IReadOnlyDictionary<DocumentId, HashSet<string>>? onlyTypeNames,
        CancellationToken cancellationToken)
    {
        var usedPaths = new Dictionary<ProjectId, HashSet<string>>();
        var plans = new List<DocumentPlan>();

        foreach (var documentId in documentIds.Distinct())
        {
            var document = solution.GetDocument(documentId);
            if (document?.Project is null)
                continue;

            HashSet<string>? filter = null;
            if (onlyTypeNames is not null && !onlyTypeNames.TryGetValue(documentId, out filter))
                continue;

            if (!usedPaths.TryGetValue(document.Project.Id, out var used))
            {
                used = UsedPaths(document.Project);
                usedPaths.Add(document.Project.Id, used);
            }

            var plan = await PlanAsync(document, filter, used, cancellationToken).ConfigureAwait(false);
            if (plan is not null)
                plans.Add(plan);
        }

        foreach (var plan in plans)
        {
            solution = solution.WithDocumentText(plan.DocumentId, plan.UpdatedText);
            var project = solution.GetProject(plan.ProjectId);
            if (project is null)
                continue;

            foreach (var added in plan.Added)
            {
                var document = project.AddDocument(added.Name, added.Text, added.Folders, added.FilePath);
                solution = document.Project.Solution;
                project = solution.GetProject(plan.ProjectId);
                if (project is null)
                    break;
            }
        }

        return solution;
    }

    private static async Task<DocumentPlan?> PlanAsync(
        Document document,
        HashSet<string>? onlyTypeNames,
        HashSet<string> usedPaths,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax unit)
            return null;

        var types = OneTypePerFileFacts.GetTopLevelTypes(unit);
        var primary = OneTypePerFileFacts.SelectPrimaryName(document.FilePath ?? document.Name, types);
        if (primary is null)
            return null;

        var moving = new List<string>();
        var movingSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            var name = OneTypePerFileFacts.GetTypeName(type);
            if (name.Length == 0 || string.Equals(name, primary, StringComparison.Ordinal))
                continue;

            if (onlyTypeNames is not null && !onlyTypeNames.Contains(name))
                continue;

            if (movingSet.Add(name))
                moving.Add(name);
        }

        if (moving.Count == 0)
            return null;

        var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var updatedRoot = RemoveTypes(unit, movingSet, stripFileMetadata: false);
        var added = new List<AddedDocument>();
        var directory = string.IsNullOrEmpty(document.FilePath)
            ? null
            : Path.GetDirectoryName(document.FilePath);

        foreach (var typeName in moving)
        {
            var extracted = RemoveTypes(unit, NamesExcept(types, typeName), stripFileMetadata: true);
            var fileName = AllocateFileName(usedPaths, directory, typeName);
            var filePath = directory is null ? null : Path.Combine(directory, fileName);
            if (filePath is not null)
                usedPaths.Add(Path.GetFullPath(filePath));

            added.Add(new AddedDocument(
                fileName,
                SourceText.From(extracted.ToFullString(), originalText.Encoding),
                document.Folders,
                filePath));
        }

        return new DocumentPlan(
            document.Id,
            document.Project.Id,
            SourceText.From(updatedRoot.ToFullString(), originalText.Encoding),
            added);
    }

    private static HashSet<string> NamesExcept(IReadOnlyList<MemberDeclarationSyntax> types, string keepName)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            var name = OneTypePerFileFacts.GetTypeName(type);
            if (name.Length == 0 || string.Equals(name, keepName, StringComparison.Ordinal))
                continue;

            names.Add(name);
        }

        return names;
    }

    private static CompilationUnitSyntax RemoveTypes(
        CompilationUnitSyntax root,
        HashSet<string> typeNames,
        bool stripFileMetadata)
    {
        var remove = OneTypePerFileFacts.GetTopLevelTypes(root)
            .Where(type => typeNames.Contains(OneTypePerFileFacts.GetTypeName(type)))
            .Cast<SyntaxNode>()
            .ToList();

        var pruned = remove.Count == 0
            ? root
            : (CompilationUnitSyntax?)root.RemoveNodes(remove, SyntaxRemoveOptions.KeepNoTrivia) ?? root;

        pruned = RemoveEmptyNamespaces(pruned);
        if (!stripFileMetadata)
            return pruned;

        var metadata = new List<SyntaxNode>();
        metadata.AddRange(pruned.AttributeLists);
        metadata.AddRange(pruned.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(IsGlobalUsing));
        if (metadata.Count == 0)
            return pruned;

        return (CompilationUnitSyntax?)pruned.RemoveNodes(metadata, SyntaxRemoveOptions.KeepNoTrivia) ?? pruned;
    }

    private static bool IsGlobalUsing(UsingDirectiveSyntax usingDirective) =>
        usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword);

    private static CompilationUnitSyntax RemoveEmptyNamespaces(CompilationUnitSyntax root)
    {
        var empty = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Where(ns => !HasTopLevelType(ns))
            .ToList();
        if (empty.Count == 0)
            return root;

        var emptySet = new HashSet<SyntaxNode>(empty);
        var outermost = empty
            .Where(ns => ns.Parent is not BaseNamespaceDeclarationSyntax parent || !emptySet.Contains(parent))
            .ToList();
        return (CompilationUnitSyntax?)root.RemoveNodes(outermost, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
    }

    private static bool HasTopLevelType(BaseNamespaceDeclarationSyntax ns)
    {
        foreach (var member in ns.Members)
        {
            if (OneTypePerFileFacts.IsTypeDeclaration(member))
                return true;

            if (member is BaseNamespaceDeclarationSyntax nested && HasTopLevelType(nested))
                return true;
        }

        return false;
    }

    private static HashSet<string> UsedPaths(Project project)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in project.Documents)
        {
            if (!string.IsNullOrEmpty(document.FilePath))
                used.Add(Path.GetFullPath(document.FilePath));
            else if (!string.IsNullOrEmpty(document.Name))
                used.Add(document.Name);
        }

        return used;
    }

    private static string AllocateFileName(HashSet<string> usedPaths, string? directory, string typeName)
    {
        var fileName = typeName + ".cs";
        if (!Conflicts(usedPaths, directory, fileName))
        {
            Remember(usedPaths, directory, fileName);
            return fileName;
        }

        for (var index = 1; ; index++)
        {
            fileName = typeName + "." + index + ".cs";
            if (!Conflicts(usedPaths, directory, fileName))
            {
                Remember(usedPaths, directory, fileName);
                return fileName;
            }
        }
    }

    private static bool Conflicts(HashSet<string> usedPaths, string? directory, string fileName)
    {
        if (directory is null)
            return usedPaths.Contains(fileName);

        return usedPaths.Contains(Path.GetFullPath(Path.Combine(directory, fileName)));
    }

    private static void Remember(HashSet<string> usedPaths, string? directory, string fileName)
    {
        if (directory is null)
            usedPaths.Add(fileName);
        else
            usedPaths.Add(Path.GetFullPath(Path.Combine(directory, fileName)));
    }

    private sealed class DocumentPlan(
        DocumentId documentId,
        ProjectId projectId,
        SourceText updatedText,
        List<AddedDocument> added)
    {
        public DocumentId DocumentId { get; } = documentId;
        public ProjectId ProjectId { get; } = projectId;
        public SourceText UpdatedText { get; } = updatedText;
        public List<AddedDocument> Added { get; } = added;
    }

    private sealed class AddedDocument(string name, SourceText text, IEnumerable<string> folders, string? filePath)
    {
        public string Name { get; } = name;
        public SourceText Text { get; } = text;
        public IEnumerable<string> Folders { get; } = folders;
        public string? FilePath { get; } = filePath;
    }
}
