namespace Novolis.Analyzers.CodeLength;

/// <summary>
/// Configurable line-count thresholds for <see cref="ClassCodeLineAnalyzer"/> and <see cref="MethodCodeLineAnalyzer"/>.
/// </summary>
public static class CodeLengthSettings
{
    /// <summary>
    /// Maximum allowed lines in a class declaration (default 250).
    /// </summary>
    public static int ClassMaxLines { get; set; } = 250;
    
    /// <summary>
    /// Maximum allowed statement lines in a method (default 50).
    /// </summary>
    public static int MethodMaxLines { get; set; } = 50;
}
