using Microsoft.CodeAnalysis;

namespace Novolis.Analyzers.CodeLength.Internals;

internal interface IDiagnosticDescriptorProvider
{
    DiagnosticDescriptor GetDescriptor();
}