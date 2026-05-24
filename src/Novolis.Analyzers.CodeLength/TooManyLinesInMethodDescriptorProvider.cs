using Novolis.Analyzers.CodeLength.Internals;
using Microsoft.CodeAnalysis;

namespace Novolis.Analyzers.CodeLength;

/// <summary>
/// Builds the <c>FRANK1010</c> diagnostic descriptor for oversized methods.
/// </summary>
public class TooManyLinesInMethodDescriptorProvider : IDiagnosticDescriptorProvider
{
    /// <inheritdoc />
    public DiagnosticDescriptor GetDescriptor() =>
        new DiagnosticDescriptorBuilder()
            .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Maintainability).WithId(10))
            .WithTitle("Too many lines in method")
            .WithMessageFormat("Method '{0}' has too many lines ({1}).")
            .WithCategory(DiagnosticCategories.Maintainability)
            .WithDefaultSeverity(DiagnosticSeverity.Warning)
            .WithIsEnabledByDefault(true)
            .WithDescription("Methods should not have too many lines.")
            .Build();
}
