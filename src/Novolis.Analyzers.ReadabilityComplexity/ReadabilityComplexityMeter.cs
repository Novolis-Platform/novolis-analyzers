using Microsoft.CodeAnalysis;

namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Public entry point for tooling that wants the same readability-complexity scores as <c>NOV2301</c>.
/// </summary>
public static class ReadabilityComplexityMeter
{
    /// <summary>Soft advisory threshold used by <c>NOV2301</c> when no editorconfig override is present.</summary>
    public const int DefaultThreshold = ReadabilityComplexityFacts.DefaultThreshold;

    /// <summary>Diagnostic id emitted by the analyzer.</summary>
    public const string DiagnosticId = ReadabilityComplexityFacts.DiagnosticId;

    /// <summary>
    /// Score one executable body with the Phase 1 readability-complexity heuristic.
    /// Nested lambdas and local functions inside <paramref name="body"/> are not folded in;
    /// score those nodes independently when needed.
    /// </summary>
    public static ReadabilityScore Score(
        SyntaxNode body,
        SemanticModel model,
        CancellationToken cancellationToken = default) =>
        ReadabilityScorer.Score(body, model, cancellationToken);
}
