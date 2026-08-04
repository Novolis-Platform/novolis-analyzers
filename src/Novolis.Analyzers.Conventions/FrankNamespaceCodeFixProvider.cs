using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.Analyzers.Conventions;

/// <summary>
/// Replaces leading <c>Frank</c> with <c>Novolis</c> in namespace and using names reported by <c>NOV2102</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FrankNamespaceCodeFixProvider)), Shared]
public sealed class FrankNamespaceCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(FrankNamespaceAnalyzer.Rule.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        NameSyntax? nameSyntax = node switch
        {
            NameSyntax name => name,
            _ => node.AncestorsAndSelf().OfType<NameSyntax>().FirstOrDefault(),
        };

        if (nameSyntax is null)
            return;

        var oldText = nameSyntax.ToString();
        if (!FrankNamespaceAnalyzer.IsFrankNamespace(oldText))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Rename Frank to Novolis",
                createChangedDocument: ct => ReplaceFrankPrefixAsync(context.Document, nameSyntax, ct),
                equivalenceKey: nameof(FrankNamespaceCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ReplaceFrankPrefixAsync(
        Document document,
        NameSyntax nameSyntax,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var oldText = nameSyntax.ToString();
        var newText = oldText.Equals("Frank", StringComparison.Ordinal)
            ? "Novolis"
            : "Novolis" + oldText.Substring("Frank".Length);

        var newName = SyntaxFactory.ParseName(newText).WithTriviaFrom(nameSyntax);
        var newRoot = root.ReplaceNode(nameSyntax, newName);
        return document.WithSyntaxRoot(newRoot);
    }
}
