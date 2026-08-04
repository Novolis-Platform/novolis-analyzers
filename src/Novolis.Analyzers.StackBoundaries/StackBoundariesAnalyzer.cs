using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Novolis.Analyzers.StackBoundaries;

/// <summary>
/// Enforces Novolis stack boundary rules: BCL numerics, no <see cref="System.Numerics.Vector2"/>,
/// camera placement, Raylib/Simulation/Rendering reference constraints, Avalonia isolation,
/// Gaming graphics islands, and Math → Physics → Simulation → Gaming → Avalonia layer ranks.
/// </summary>
/// <remarks>
/// Diagnostic IDs: <c>NOV2001</c> duplicate numerics, <c>NOV2002</c> Vector2, <c>NOV2003</c> camera in Math,
/// <c>NOV2004</c> Raylib/Simulation cross-refs, <c>NOV2005</c> Raylib rendering scene refs,
/// <c>NOV2006</c> Avalonia refs outside Avalonia layer, <c>NOV2007</c> layer inversion,
/// <c>NOV2008</c> Rendering/Simulation cross-refs, <c>NOV2009</c> Gaming must not ref Raylib/Rendering.
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

    private static readonly DiagnosticDescriptor AvaloniaOutsideLayerRule = new(
        "NOV2006",
        "Only Novolis.Avalonia.* libraries may depend on Avalonia",
        "Assembly '{0}' must not reference '{1}' — Avalonia UI packages are reserved for Novolis.Avalonia.* (apps compose Avalonia at the product layer)",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor LayerInversionRule = new(
        "NOV2007",
        "Lower stack layers must not reference higher layers",
        "Assembly '{0}' (layer {1}) must not reference '{2}' (layer {3}) — dependency direction is Math → Physics → Simulation → Gaming → Avalonia → Apps",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor RenderingSimulationRefRule = new(
        "NOV2008",
        "Rendering and Simulation must not reference each other",
        "Assembly '{0}' must not reference '{1}' — wire Rendering ↔ Simulation only in apps",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor GamingGraphicsIslandRule = new(
        "NOV2009",
        "Gaming must not reference Raylib or Rendering",
        "Assembly '{0}' must not reference '{1}' — Novolis.Game.* stays graphics-free; apps compose Raylib/Rendering",
        "Novolis.Stack",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DuplicateNumericsRule,
        Vector2Rule,
        CameraInMathRule,
        RaylibSimulationRefRule,
        RaylibRenderingSceneRefRule,
        AvaloniaOutsideLayerRule,
        LayerInversionRule,
        RenderingSimulationRefRule,
        GamingGraphicsIslandRule,
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName, SyntaxKind.QualifiedName);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static bool IsNumericStackAssembly(Compilation compilation)
    {
        var name = compilation.AssemblyName ?? string.Empty;
        return name.StartsWith("Novolis.Math.", StringComparison.Ordinal)
            || name.StartsWith("Novolis.Physics.", StringComparison.Ordinal)
            || name.StartsWith("Novolis.Simulation.", StringComparison.Ordinal);
    }

    private static bool IsNovolisLibraryAssembly(string assemblyName) =>
        assemblyName.StartsWith("Novolis.", StringComparison.Ordinal)
        && !assemblyName.EndsWith(".Unit", StringComparison.Ordinal)
        && !assemblyName.EndsWith(".Tests", StringComparison.Ordinal)
        && !assemblyName.Contains(".Unit.", StringComparison.Ordinal);

    /// <summary>
    /// Spine ranks (low → high). Null = not on the closed spine (still subject to Avalonia isolation).
    /// </summary>
    private static int? GetSpineRank(string assemblyName)
    {
        if (assemblyName.StartsWith("Novolis.Math.", StringComparison.Ordinal)
            || assemblyName.Equals("Novolis.Math", StringComparison.Ordinal))
        {
            return 0;
        }

        if (assemblyName.StartsWith("Novolis.Physics.", StringComparison.Ordinal)
            || assemblyName.Equals("Novolis.Physics", StringComparison.Ordinal))
        {
            return 1;
        }

        if (assemblyName.StartsWith("Novolis.Simulation.", StringComparison.Ordinal)
            || assemblyName.Equals("Novolis.Simulation", StringComparison.Ordinal))
        {
            return 2;
        }

        if (assemblyName.StartsWith("Novolis.Game.", StringComparison.Ordinal)
            || assemblyName.Equals("Novolis.Game", StringComparison.Ordinal))
        {
            return 3;
        }

        if (assemblyName.StartsWith("Novolis.Avalonia.", StringComparison.Ordinal)
            || assemblyName.Equals("Novolis.Avalonia", StringComparison.Ordinal))
        {
            return 4;
        }

        return null;
    }

    private static string SpineLayerName(int rank) => rank switch
    {
        0 => "Math",
        1 => "Physics",
        2 => "Simulation",
        3 => "Gaming",
        4 => "Avalonia",
        _ => rank.ToString(),
    };

    private static bool IsAvaloniaAssembly(string refName) =>
        refName.Equals("Avalonia", StringComparison.Ordinal)
        || refName.StartsWith("Avalonia.", StringComparison.Ordinal);

    private static bool IsAvaloniaLayerAssembly(string assemblyName) =>
        assemblyName.StartsWith("Novolis.Avalonia.", StringComparison.Ordinal)
        || assemblyName.Equals("Novolis.Avalonia", StringComparison.Ordinal);

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var self = context.Compilation.AssemblyName ?? string.Empty;
        var selfRank = GetSpineRank(self);

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

            // NOV2006: only Novolis.Avalonia.* libraries may take Avalonia UI package refs.
            if (IsNovolisLibraryAssembly(self)
                && !IsAvaloniaLayerAssembly(self)
                && IsAvaloniaAssembly(refName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AvaloniaOutsideLayerRule,
                    Location.None,
                    self,
                    refName));
            }

            // NOV2007: closed spine — lower must not reference higher.
            var refRank = GetSpineRank(refName);
            if (selfRank is int s && refRank is int r && s < r)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    LayerInversionRule,
                    Location.None,
                    self,
                    SpineLayerName(s),
                    refName,
                    SpineLayerName(r)));
            }

            // NOV2008: Rendering ↔ Simulation forbidden both ways.
            if ((IsRenderingAssembly(self) && IsSimulationAssembly(refName))
                || (IsSimulationAssembly(self) && IsRenderingAssembly(refName)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RenderingSimulationRefRule,
                    Location.None,
                    self,
                    refName));
            }

            // NOV2009: Gaming must not reference Raylib or Rendering.
            if (IsGamingAssembly(self)
                && (IsRaylibAssembly(refName) || IsRenderingAssembly(refName)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GamingGraphicsIslandRule,
                    Location.None,
                    self,
                    refName));
            }
        }
    }

    private static bool IsSimulationAssembly(string name) =>
        name.StartsWith("Novolis.Simulation.", StringComparison.Ordinal)
        || name.Equals("Novolis.Simulation", StringComparison.Ordinal);

    private static bool IsRenderingAssembly(string name) =>
        name.StartsWith("Novolis.Rendering.", StringComparison.Ordinal)
        || name.Equals("Novolis.Rendering", StringComparison.Ordinal);

    private static bool IsRaylibAssembly(string name) =>
        name.StartsWith("Novolis.Raylib.", StringComparison.Ordinal)
        || name.Equals("Novolis.Raylib", StringComparison.Ordinal);

    private static bool IsGamingAssembly(string name) =>
        name.StartsWith("Novolis.Game.", StringComparison.Ordinal)
        || name.Equals("Novolis.Game", StringComparison.Ordinal);

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
        if (!IsNumericStackAssembly(context.Compilation))
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
        if (!IsNumericStackAssembly(context.Compilation))
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
