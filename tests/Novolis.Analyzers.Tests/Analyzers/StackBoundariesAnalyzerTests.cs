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

    [Test]
    public async Task SimulationReferencingAvalonia_ReportsNov2006()
    {
        var avaloniaRef = CSharpCompilation.Create("Avalonia")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Simulation.Core { public static class Sim { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.Core",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), avaloniaRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2006")).IsTrue();
    }

    [Test]
    public async Task GameReferencingAvalonia_ReportsNov2006()
    {
        var avaloniaRef = CSharpCompilation.Create("Avalonia.Controls")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Game.Identity { public static class Id { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Game.Identity",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), avaloniaRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2006")).IsTrue();
    }

    [Test]
    public async Task AvaloniaLayerReferencingAvalonia_DoesNotReportNov2006()
    {
        var avaloniaRef = CSharpCompilation.Create("Avalonia")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Avalonia.Controls { public static class C { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Avalonia.Controls",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), avaloniaRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2006")).IsFalse();
    }

    [Test]
    public async Task MathReferencingSimulation_ReportsNov2007()
    {
        var simRef = CSharpCompilation.Create("Novolis.Simulation.Core")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Math.Geometry { public static class G { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Math.Geometry",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2007")).IsTrue();
    }

    [Test]
    public async Task SimulationReferencingGame_ReportsNov2007()
    {
        var gameRef = CSharpCompilation.Create("Novolis.Game.Identity")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Simulation.Core { public static class Sim { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.Core",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), gameRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2007")).IsTrue();
    }

    [Test]
    public async Task GameReferencingSimulation_DoesNotReportNov2007()
    {
        var simRef = CSharpCompilation.Create("Novolis.Simulation.Humanoid")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Game.Humanoid { public static class H { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Game.Humanoid",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2007")).IsFalse();
    }

    [Test]
    public async Task AvaloniaReferencingGame_DoesNotReportNov2007()
    {
        var gameRef = CSharpCompilation.Create("Novolis.Game.Identity")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Avalonia.Studio { public static class S { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Avalonia.Studio",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), gameRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2007")).IsFalse();
    }

    [Test]
    public async Task RenderingReferencingSimulation_ReportsNov2008()
    {
        var simRef = CSharpCompilation.Create("Novolis.Simulation.Core")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Rendering.Scene { public static class S { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Rendering.Scene",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2008")).IsTrue();
    }

    [Test]
    public async Task SimulationReferencingRendering_ReportsNov2008()
    {
        var renderingRef = CSharpCompilation.Create("Novolis.Rendering.Scene")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Simulation.Core { public static class S { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.Core",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), renderingRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2008")).IsTrue();
    }

    [Test]
    public async Task GameReferencingRaylib_ReportsNov2009()
    {
        var raylibRef = CSharpCompilation.Create("Novolis.Raylib.Game")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Game.Identity { public static class Id { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Game.Identity",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), raylibRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2009")).IsTrue();
    }

    [Test]
    public async Task GameReferencingRendering_ReportsNov2009()
    {
        var renderingRef = CSharpCompilation.Create("Novolis.Rendering.Scene")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Game.Humanoid { public static class H { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Game.Humanoid",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), renderingRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2009")).IsTrue();
    }

    [Test]
    public async Task GameReferencingSimulation_DoesNotReportNov2009()
    {
        var simRef = CSharpCompilation.Create("Novolis.Simulation.Humanoid")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Game.Humanoid { public static class H { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Game.Humanoid",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2009")).IsFalse();
    }

    [Test]
    public async Task PhysicsReferencingGame_ReportsNov2007_WithPhysicsLabel()
    {
        var gameRef = CSharpCompilation.Create("Novolis.Game.Identity")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Physics.Core { public static class P { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Physics.Core",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), gameRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        var hit = diagnostics.Single(d => d.Id == "NOV2007");
        await Assert.That(hit.GetMessage()).Contains("Physics");
    }

    [Test]
    public async Task MathReferencingAvalonia_ReportsNov2007_WithAvaloniaLabel()
    {
        var avaloniaRef = CSharpCompilation.Create("Novolis.Avalonia.Controls")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Math.Geometry { public static class G { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Math.Geometry",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), avaloniaRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        var hit = diagnostics.Single(d => d.Id == "NOV2007");
        await Assert.That(hit.GetMessage()).Contains("Avalonia");
    }

    [Test]
    public async Task RaylibReferencingRenderingMaterials_ReportsNov2005()
    {
        var materialsRef = CSharpCompilation.Create("Novolis.Rendering.Materials")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Raylib.Runtime { public static class Shell { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Raylib.Runtime",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), materialsRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2005")).IsTrue();
    }

    [Test]
    public async Task DuplicateNumericsTypeNames_ReportNov2001()
    {
        foreach (var typeName in new[] { "Vector3D", "Quaterniond", "QuaternionD", "Matrix4x4d", "Vector2D" })
        {
            var code = $$"""
                          namespace Novolis.Math.Geometry;

                          public struct {{typeName}}
                          {
                              public double X;
                          }
                          """;
            var diagnostics = await AnalyzeMathAssemblyAsync(code);
            await Assert.That(diagnostics.Any(d => d.Id == "NOV2001")).IsTrue();
        }
    }

    [Test]
    public async Task CameraOutsideMath_DoesNotReportNov2003()
    {
        const string code = """
                              namespace Novolis.Simulation.View;

                              public sealed class Camera
                              {
                              }
                              """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.View",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2003")).IsFalse();
    }

    [Test]
    public async Task TestAssembly_MayReferenceAvalonia_WithoutNov2006()
    {
        var avaloniaRef = CSharpCompilation.Create("Avalonia")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Simulation.Core { public static class Sim { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Simulation.Core.Tests",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), avaloniaRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2006")).IsFalse();
    }

    [Test]
    public async Task ExactSpineAssemblyNames_EnforceBoundaries()
    {
        var simRef = CSharpCompilation.Create("Novolis.Simulation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToMetadataReference();

        var tree = CSharpSyntaxTree.ParseText("namespace Novolis.Math { public static class G { } }");
        var compilation = CSharpCompilation.Create(
            "Novolis.Math",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), simRef]);

        var diagnostics = await AnalyzeCompilationAsync(compilation);
        await Assert.That(diagnostics.Any(d => d.Id == "NOV2007")).IsTrue();
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
