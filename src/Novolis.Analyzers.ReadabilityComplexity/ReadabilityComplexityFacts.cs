namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Shared identifiers and defaults for <c>NOV2301</c>.
/// </summary>
internal static class ReadabilityComplexityFacts
{
    public const string DiagnosticId = "NOV2301";

    public const string Category = "Novolis.CodeQuality";

    /// <summary>
    /// Soft advisory threshold. Builds keep compiling because governance lists NOV2301 in WarningsNotAsErrors.
    /// </summary>
    public const int DefaultThreshold = 20;

    public const string ThresholdEditorConfigKey = "novolis_readability_complexity_threshold";

    public const string ScoreProperty = "score";

    public const string BreakdownProperty = "breakdown";
}
