using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.StackBoundaries;

/// <summary>
/// Enforces Novolis stack boundary rules: BCL numerics, no <see cref="System.Numerics.Vector2"/>, camera placement, and Raylib/Simulation/rendering reference constraints.
/// </summary>
/// <remarks>
/// Diagnostic IDs: <c>NOV2001</c> duplicate numerics, <c>NOV2002</c> Vector2, <c>NOV2003</c> camera in Math,
/// <c>NOV2004</c> Raylib/Simulation cross-refs, <c>NOV2005</c> Raylib rendering scene refs.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StackBoundariesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DuplicateNumericsRule = new(
        "NOV2001",
        "Do not duplicate BCL numerics types",
        "Type '{0}' mirrors System.Numerics; use BCL types in the Novolis stack",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor Vector2Rule = new(
        "NOV2002",
        "Vector2 is forbidden in the Novolis stack",
        "Use System.Numerics.Vector3 with Y = 0 for planar XZ",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CameraInMathRule = new(
        "NOV2003",
        "Camera belongs in Simulation.View",
        "Move camera types to Novolis.Simulation.View",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor RaylibSimulationRefRule = new(
        "NOV2004",
        "Raylib and Simulation must not reference each other",
        "Assembly '{0}' must not reference '{1}'",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor RaylibRenderingSceneRefRule = new(
        "NOV2005",
        "Raylib must not reference rendering scene packages",
        "Assembly '{0}' must not reference '{1}' (scene/material types belong in novolis-rendering only)",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DuplicateNumericsRule, Vector2Rule, CameraInMathRule, RaylibSimulationRefRule, RaylibRenderingSceneRefRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName, SyntaxKind.QualifiedName);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static bool IsStackAssembly(Compilation compilation)
    {
        var name = compilation.AssemblyName ?? string.Empty;
        return name.StartsWith("Novolis.Math.", StringComparison.Ordinal)
            || name.StartsWith("Novolis.Physics.", StringComparison.Ordinal)
            || name.StartsWith("Novolis.Simulation.", StringComparison.Ordinal);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var self = context.Compilation.AssemblyName ?? string.Empty;
        foreach (var reference in context.Compilation.ReferencedAssemblyNames)
        {
            var refName = reference.Name ?? string.Empty;
            if (self.StartsWith("Novolis.Raylib.", StringComparison.Ordinal)
                && refName.StartsWith("Novolis.Simulation.", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RaylibSimulationRefRule,
                    Location.None,
                    self,
                    refName));
            }

            if (self.StartsWith("Novolis.Simulation.", StringComparison.Ordinal)
                && refName.StartsWith("Novolis.Raylib.", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RaylibSimulationRefRule,
                    Location.None,
                    self,
                    refName));
            }

            if (self.StartsWith("Novolis.Raylib.", StringComparison.Ordinal)
                && IsRenderingSceneAssembly(refName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RaylibRenderingSceneRefRule,
                    Location.None,
                    self,
                    refName));
            }
        }
    }

    private static bool IsRenderingSceneAssembly(string refName) =>
        refName is "Novolis.Rendering.Scene"
            or "Novolis.Rendering.Materials"
            or "Novolis.Rendering.Compile"
            or "Novolis.Rendering.Backends.Cpu"
            or "Novolis.Rendering.Backends.Igpu"
            or "Novolis.Rendering.Backends.Vulkan"
            or "Novolis.Rendering.DependencyInjection";

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        if (!IsStackAssembly(context.Compilation))
            return;

        if (context.Symbol is not INamedTypeSymbol type)
            return;

        if (type.GetAttributes().Any(a => a.AttributeClass?.Name is "ObsoleteAttribute" or "Obsolete"))
            return;

        if (type.DeclaringSyntaxReferences.Length == 0)
            return;

        var syntax = type.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
        if (syntax is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl
            && classDecl.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("Obsolete", StringComparison.Ordinal)))
            return;

        if (syntax is StructDeclarationSyntax { AttributeLists.Count: > 0 } structDecl
            && structDecl.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("Obsolete", StringComparison.Ordinal)))
            return;

        var name = type.Name;
        if (name is "Vector3d" or "Vector3D" or "Quaterniond" or "QuaternionD" or "Matrix4x4d"
            or "Vector2d" or "Vector2D")
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DuplicateNumericsRule,
                type.Locations[0],
                name));
        }

        if (name == "Camera"
            && type.ContainingNamespace?.ToDisplayString().StartsWith("Novolis.Math", StringComparison.Ordinal) == true)
        {
            context.ReportDiagnostic(Diagnostic.Create(CameraInMathRule, type.Locations[0]));
        }
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (!IsStackAssembly(context.Compilation))
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(context.Node, context.CancellationToken);
        var type = typeInfo.Type ?? typeInfo.ConvertedType;
        if (type is null)
            return;

        if (type.ToDisplayString() == "System.Numerics.Vector2")
        {
            context.ReportDiagnostic(Diagnostic.Create(Vector2Rule, context.Node.GetLocation()));
        }
    }
}
