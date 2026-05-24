using Novolis.Analyzers.CodeLength.Internals;
using Microsoft.CodeAnalysis;

namespace Novolis.Analyzers.CodeLength;

/// <summary>
/// Builds the <c>FRANK1011</c> diagnostic descriptor for oversized classes.
/// </summary>
public class TooManyLinesInClassDescriptorProvider : IDiagnosticDescriptorProvider
{
    /// <inheritdoc />
    public DiagnosticDescriptor GetDescriptor() =>
        new DiagnosticDescriptorBuilder()
            .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Maintainability).WithId(11))
            .WithTitle("Too many lines in class")
            .WithMessageFormat("Class '{0}' has too many lines ({1}).")
            .WithCategory(DiagnosticCategories.Maintainability)
            .WithDefaultSeverity(DiagnosticSeverity.Warning)
            .WithIsEnabledByDefault(true)
            .WithDescription("Classes should not have too many lines.")
            .Build();
}
