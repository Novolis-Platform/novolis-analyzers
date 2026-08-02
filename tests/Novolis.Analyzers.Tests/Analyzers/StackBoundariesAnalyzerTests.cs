using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Novolis.Analyzers.StackBoundaries;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

public sealed class StackBoundariesAnalyzerTests
{
    [Test]
    public async Task Vector2_InMathAssembly_ReportsNov2002()
    {
        const string code = """
                              using System.Numerics;

                              namespace Novolis.Math.Geometry;

                              public static class Planar
                              {
                                  public static float Area(Vector2 v) => v.X * v.Y;
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2002")).IsTrue();
    }

    [Test]
    public async Task DuplicateNumericsType_ReportsNov2001()
    {
        const string code = """
                              namespace Novolis.Math.Geometry;

                              public struct Vector3d
                              {
                                  public double X;
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsTrue();
    }

    [Test]
    public async Task CameraInMathNamespace_ReportsNov2003()
    {
        const string code = """
                              namespace Novolis.Math.Geometry;

                              public sealed class Camera
                              {
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2003")).IsTrue();
    }

    [Test]
    public async Task RaylibReferencingSimulation_ReportsNov2004()
    {
        var simulationRef = CSharpCompilation.Create("Novolis.Simulation.Core")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Raylib.Game { public static class GameApp { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Raylib.Game",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simulationRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2004")).IsTrue();
    }

    [Test]
    public async Task RaylibReferencingRenderingScene_ReportsNov2005()
    {
        var sceneRef = CSharpCompilation.Create("Novolis.Rendering.Scene")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Raylib.Runtime { public static class Shell { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Raylib.Runtime",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), sceneRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2005")).IsTrue();
    }

    [Test]
    public async Task SimulationReferencingRaylib_ReportsNov2004()
    {
        var raylibRef = CSharpCompilation.Create("Novolis.Raylib.Game")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Simulation.Core { public static class SimApp { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.Core",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), raylibRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2004")).IsTrue();
    }

    [Test]
    public async Task ObsoleteDuplicateNumericsType_DoesNotReportNov2001()
    {
        const string code = """
                              using System;

                              namespace Novolis.Math.Geometry;

                              [Obsolete("Use System.Numerics instead")]
                              public struct Vector3d
                              {
                                  public double X;
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsFalse();
    }

    [Test]
    public async Task ObsoleteClassDuplicateNumericsType_DoesNotReportNov2001()
    {
        const string code = """
                              using System;

                              namespace Novolis.Math.Geometry;

                              [Obsolete("Use System.Numerics instead")]
                              public class Vector3d
                              {
                                  public double X;
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsFalse();
    }

    [Test]
    public async Task ObsoleteStructDuplicateNumericsType_DoesNotReportNov2001()
    {
        const string code = """
                              using System;

                              namespace Novolis.Math.Geometry;

                              [Obsolete("Use System.Numerics instead")]
                              public struct Vector2d
                              {
                                  public double X;
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsFalse();
    }

    [Test]
    public async Task CustomObsoleteSyntaxOnClass_DoesNotReportNov2001()
    {
        const string code = """
                              using System;

                              namespace Novolis.Math.Geometry;

                              [ObsoleteSyntaxMarker]
                              public class Vector3d
                              {
                                  public double X;
                              }

                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                              public sealed class ObsoleteSyntaxMarkerAttribute : Attribute
                              {
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsFalse();
    }

    [Test]
    public async Task CustomObsoleteSyntaxOnStruct_DoesNotReportNov2001()
    {
        const string code = """
                              using System;

                              namespace Novolis.Math.Geometry;

                              [ObsoleteSyntaxMarker]
                              public struct Vector2d
                              {
                                  public double X;
                              }

                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                              public sealed class ObsoleteSyntaxMarkerAttribute : Attribute
                              {
                              }
                              """;

        var diagnostics = await AnalyzeMathAssemblyAsync(code);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsFalse();
    }

    [Test]
    public async Task PhysicsAssembly_WithVector2_ReportsNov2002()
    {
        const string code = """
                              using System.Numerics;

                              namespace Novolis.Physics.Core;

                              public static class Planar
                              {
                                  public static float Area(Vector2 v) => v.X * v.Y;
                              }
                              """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Numerics.Vector2).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Novolis.Physics.Core", [tree], references);
        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2002")).IsTrue();
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeMathAssemblyAsync(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Numerics.Vector2).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create("Novolis.Math.Geometry", [tree], references);
        return await AnalyzeCompilationAsync(compilation);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeCompilationAsync(Compilation compilation)
    {
        var analyzer = new StackBoundariesAnalyzer();
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }
}
