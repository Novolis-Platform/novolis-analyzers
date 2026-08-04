using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Novolis.Analyzers.Conventions;

/// <summary>
/// Reports the forbidden whole-word <c>desk</c> in identifiers, string literals, and comments (<c>NOV2101</c>).
/// Allows the substring inside <c>desktop</c> / <c>Desktop</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeskWordAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex DeskWordInText = new(
        @"\bdesk\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly DiagnosticDescriptor IdentifierRule = new(
        "NOV2101",
        "Forbidden word 'desk'",
        "Identifier '{0}' contains the forbidden word 'desk'. Prefer bridge / books / session / UI / shell / studio depending on domain (never help desk).",
        "Novolis.Conventions",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LiteralRule = new(
        "NOV2101",
        "Forbidden word 'desk'",
        "String literal contains the forbidden word 'desk'. Prefer bridge / books / session / UI / shell / studio depending on domain.",
        "Novolis.Conventions",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CommentRule = new(
        "NOV2101",
        "Forbidden word 'desk'",
        "Comment contains the forbidden word 'desk'. Prefer bridge / books / session / UI / shell / studio depending on domain.",
        "Novolis.Conventions",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [IdentifierRule, LiteralRule, CommentRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxTreeAction(AnalyzeComments);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void AnalyzeMethod(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void AnalyzeProperty(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void AnalyzeField(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void AnalyzeEvent(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void AnalyzeParameter(SymbolAnalysisContext context) =>
        ReportIfDeskIdentifier(context, context.Symbol);

    private static void ReportIfDeskIdentifier(SymbolAnalysisContext context, ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
            return;

        if (!IdentifierContainsDeskSegment(symbol.Name))
            return;

        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(IdentifierRule, location, symbol.Name));
    }

    private static void AnalyzeLiteral(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not LiteralExpressionSyntax literal)
            return;

        if (!TextContainsDeskWord(literal.Token.ValueText))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LiteralRule, literal.GetLocation()));
    }

    private static void AnalyzeComments(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var trivia in root.DescendantTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                && !trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                continue;
            }

            var text = trivia.ToFullString();
            foreach (Match match in DeskWordInText.Matches(text))
            {
                var span = new TextSpan(trivia.SpanStart + match.Index, match.Length);
                var location = Location.Create(context.Tree, span);
                context.ReportDiagnostic(Diagnostic.Create(CommentRule, location));
            }
        }
    }

    /// <summary>
    /// True when a PascalCase / snake_case identifier segment equals <c>desk</c> (not <c>desktop</c>).
    /// </summary>
    internal static bool IdentifierContainsDeskSegment(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        foreach (var segment in SplitIdentifierSegments(name))
        {
            if (segment.Equals("desk", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static bool TextContainsDeskWord(string text) =>
        !string.IsNullOrEmpty(text) && DeskWordInText.IsMatch(text);

    internal static IEnumerable<string> SplitIdentifierSegments(string name)
    {
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '_' or '-')
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                continue;
            }

            if (char.IsDigit(c))
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                continue;
            }

            if (char.IsUpper(c) && current.Length > 0)
            {
                // Start of a new PascalCase segment (Desktop -> Desktop as one if all caps run handled below)
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                var prevIsLower = char.IsLower(current[current.Length - 1]);
                if (prevIsLower || nextIsLower)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }
}
