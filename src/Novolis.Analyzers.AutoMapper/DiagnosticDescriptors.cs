using Microsoft.CodeAnalysis;

namespace Novolis.Analyzers.AutoMapper;

/// <summary>
/// Diagnostic descriptors for <see cref="AutoMapperMapAnalyzer"/> (rule <c>AUTO001</c>).
/// </summary>
public static class DiagnosticDescriptors
{
    /// <summary>
    /// Reports when AutoMapper <c>Map&lt;&gt;()</c> is not called with two generic type arguments.
    /// </summary>
    public static readonly DiagnosticDescriptor AutoMapperMap = new DiagnosticDescriptor(
        "AUTO001",
        "Maintainability",
        ".Map<>() should be used with two generic arguments",
        "AutoMapper .Map<>() should be used with two generic arguments.",
        DiagnosticSeverity.Error,
        true);
}
