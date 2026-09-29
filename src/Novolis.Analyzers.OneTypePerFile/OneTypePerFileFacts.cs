using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.Analyzers.OneTypePerFile;

/// <summary>Shared syntax facts for <c>NOV2201</c>.</summary>
internal static class OneTypePerFileFacts
{
    internal const string DiagnosticId = "NOV2201";
    internal const string TypeNameProperty = "TypeName";
    internal const string EquivalenceKey = "MoveExtraTypeToOwnFile";

    internal static bool IsTypeDeclaration(MemberDeclarationSyntax member) =>
        member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;

    internal static string GetTypeName(MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax declaration => declaration.Identifier.ValueText,
        _ => string.Empty,
    };

    internal static Location GetIdentifierLocation(MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.GetLocation(),
        DelegateDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
        _ => member.GetLocation(),
    };

    internal static List<MemberDeclarationSyntax> GetTopLevelTypes(CompilationUnitSyntax root)
    {
        var types = new List<MemberDeclarationSyntax>();
        Collect(root.Members, types);
        return types;
    }

    internal static string? SelectPrimaryName(string? filePath, IReadOnlyList<MemberDeclarationSyntax> types)
    {
        if (types.Count == 0)
            return null;

        var fileName = string.IsNullOrEmpty(filePath)
            ? null
            : Path.GetFileNameWithoutExtension(filePath);

        if (fileName is not null)
        {
            var exact = types.FirstOrDefault(type =>
                string.Equals(GetTypeName(type), fileName, StringComparison.Ordinal));
            if (exact is not null)
                return GetTypeName(exact);

            var ignoreCase = types.FirstOrDefault(type =>
                string.Equals(GetTypeName(type), fileName, StringComparison.OrdinalIgnoreCase));
            if (ignoreCase is not null)
                return GetTypeName(ignoreCase);
        }

        return GetTypeName(types[0]);
    }

    internal static string FileDisplayName(string? filePath) =>
        string.IsNullOrEmpty(filePath) ? "this file" : Path.GetFileName(filePath);

    private static void Collect(SyntaxList<MemberDeclarationSyntax> members, List<MemberDeclarationSyntax> types)
    {
        foreach (var member in members)
        {
            if (IsTypeDeclaration(member))
                types.Add(member);
            else if (member is BaseNamespaceDeclarationSyntax ns)
                Collect(ns.Members, types);
        }
    }
}
