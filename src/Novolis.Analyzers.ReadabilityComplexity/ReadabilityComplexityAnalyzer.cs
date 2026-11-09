using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Warns when an executable body's readability-complexity score exceeds the advisory threshold (<c>NOV2301</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadabilityComplexityAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        ReadabilityComplexityFacts.DiagnosticId,
        "Method readability complexity is high",
        "'{0}' has a readability complexity score of {1} (threshold {2}), primarily from {3}",
        ReadabilityComplexityFacts.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "Estimates cognitive bookkeeping from local declarations, decisions, consumed results, and explicit non-local mutation. " +
        "It is not cyclomatic complexity. Warning only; not a build gate.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeMethodLike,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.LocalFunctionStatement);
        context.RegisterSyntaxNodeAction(
            AnalyzeAccessor,
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration);
        context.RegisterSyntaxNodeAction(
            AnalyzeLambda,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeMethodLike(SyntaxNodeAnalysisContext context)
    {
        var threshold = GetThreshold(context);
        switch (context.Node)
        {
            case MethodDeclarationSyntax method:
                AnalyzeBody(context, method.Identifier.ValueText, method.Body, method.ExpressionBody, method.Identifier.GetLocation(), threshold);
                break;
            case ConstructorDeclarationSyntax ctor:
                AnalyzeBody(context, ctor.Identifier.ValueText, ctor.Body, ctor.ExpressionBody, ctor.Identifier.GetLocation(), threshold);
                break;
            case DestructorDeclarationSyntax dtor:
                AnalyzeBody(context, dtor.Identifier.ValueText, dtor.Body, dtor.ExpressionBody, dtor.Identifier.GetLocation(), threshold);
                break;
            case OperatorDeclarationSyntax op:
                AnalyzeBody(context, op.OperatorToken.ValueText, op.Body, op.ExpressionBody, op.OperatorToken.GetLocation(), threshold);
                break;
            case ConversionOperatorDeclarationSyntax conversion:
                AnalyzeBody(context, "conversion", conversion.Body, conversion.ExpressionBody, conversion.Type.GetLocation(), threshold);
                break;
            case LocalFunctionStatementSyntax local:
                AnalyzeBody(context, local.Identifier.ValueText, local.Body, local.ExpressionBody, local.Identifier.GetLocation(), threshold);
                break;
        }
    }

    private static void AnalyzeAccessor(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AccessorDeclarationSyntax accessor)
            return;

        var name = accessor.Keyword.ValueText;
        if (accessor.Parent?.Parent is PropertyDeclarationSyntax property)
            name = property.Identifier.ValueText + "." + name;
        else if (accessor.Parent?.Parent is IndexerDeclarationSyntax)
            name = "this." + name;
        else if (accessor.Parent?.Parent is EventDeclarationSyntax evt)
            name = evt.Identifier.ValueText + "." + name;

        AnalyzeBody(context, name, accessor.Body, accessor.ExpressionBody, accessor.Keyword.GetLocation(), GetThreshold(context));
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        // Only score lambdas with a block body; expression lambdas are usually trivial bindings.
        switch (context.Node)
        {
            case SimpleLambdaExpressionSyntax { Block: { } block } lambda:
                AnalyzeBody(context, "lambda", block, expressionBody: null, lambda.GetLocation(), GetThreshold(context));
                break;
            case ParenthesizedLambdaExpressionSyntax { Block: { } block } lambda:
                AnalyzeBody(context, "lambda", block, expressionBody: null, lambda.GetLocation(), GetThreshold(context));
                break;
            case AnonymousMethodExpressionSyntax { Block: { } block } anonymous:
                AnalyzeBody(context, "anonymous method", block, expressionBody: null, anonymous.GetLocation(), GetThreshold(context));
                break;
        }
    }

    private static void AnalyzeBody(
        SyntaxNodeAnalysisContext context,
        string name,
        BlockSyntax? body,
        ArrowExpressionClauseSyntax? expressionBody,
        Location location,
        int threshold)
    {
        SyntaxNode? root = body;
        if (root is null && expressionBody is not null)
            root = expressionBody.Expression;

        if (root is null)
            return;

        var score = ReadabilityScorer.Score(root, context.SemanticModel, context.CancellationToken);
        if (score.Total < threshold)
            return;

        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ReadabilityComplexityFacts.ScoreProperty, score.Total.ToString())
            .Add(ReadabilityComplexityFacts.BreakdownProperty, score.FormatBreakdown());

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            properties,
            name,
            score.Total,
            threshold,
            score.FormatPrimaryCauses()));
    }

    private static int GetThreshold(SyntaxNodeAnalysisContext context)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (options.TryGetValue(ReadabilityComplexityFacts.ThresholdEditorConfigKey, out var raw)
            && int.TryParse(raw, out var configured)
            && configured > 0)
        {
            return configured;
        }

        return ReadabilityComplexitySettings.Threshold;
    }
}
