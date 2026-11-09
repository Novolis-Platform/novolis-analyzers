namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Mutable defaults for tests. Production reads threshold from .editorconfig when present.
/// </summary>
public static class ReadabilityComplexitySettings
{
    /// <summary>
    /// Soft warning threshold for <c>NOV2301</c>. Override in tests; prefer editorconfig in products.
    /// </summary>
    public static int Threshold { get; set; } = ReadabilityComplexityFacts.DefaultThreshold;
}
