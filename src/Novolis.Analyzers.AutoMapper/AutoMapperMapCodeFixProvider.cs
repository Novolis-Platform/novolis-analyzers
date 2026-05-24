using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.Analyzers.AutoMapper;

/// <summary>
/// Code fix provider that adds a missing source type argument to AutoMapper <c>Map&lt;Destination&gt;()</c> invocations.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoMapperMapCodeFixProvider)), Shared]
public class AutoMapperMapCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
        ImmutableArray.Create(DiagnosticDescriptors.AutoMapperMap.Id);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

        if (invocation == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add missing type argument",
                createChangedDocument: c => AddMissingTypeArgumentAsync(context.Document, invocation, c),
                equivalenceKey: nameof(AutoMapperMapCodeFixProvider)),
            diagnostic);
    }

    private async Task<Document> AddMissingTypeArgumentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        
        // Extract the type of the argument passed to Map()
        var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (argument is null)
        {
            return document;
        }

        var argumentType = semanticModel.GetTypeInfo(argument.Expression, cancellationToken).Type;
        if (argumentType is null)
        {
            return document;
        }

        if (invocation.Expression is not GenericNameSyntax genericName)
        {
            return document;
        }

        // Construct a new generic type argument list with the inferred source type
        var newGenericName = genericName.WithTypeArgumentList(
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList<TypeSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        genericName.TypeArgumentList.Arguments.First(), // The existing Destination type
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.ParseTypeName(argumentType.ToDisplayString()) // Inferred Source type
                    }
                )
            )
        );

        var newInvocation = invocation.WithExpression(newGenericName);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
