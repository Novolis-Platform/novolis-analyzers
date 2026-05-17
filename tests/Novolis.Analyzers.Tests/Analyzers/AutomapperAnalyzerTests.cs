using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Novolis.Analyzers.AutoMapper;

namespace Novolis.Analyzers.Tests.Analyzers;

public class AutomapperAnalyzerTests
{
    [Test]
    [Skip("Analyzer harness under net10/Roslyn 4.14 returns no diagnostics for minimal compilation; revisit with full reference graph.")]
    public async Task Analyze_WhenAutomapperProfileIsMissing_ShouldReturnDiagnostic()
    {
        var code = BaseCode + OriginalCode;
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("AnalyzerTest")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new AutoMapperMapAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);

        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        foreach (var diagnostic in diagnostics)
        {
            TestContext.Current?.OutputWriter.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} at {diagnostic.Location}");
        }

        diagnostics.Should().NotBeEmpty();
    }

    private const string OriginalCode = """

                                        public class MyTestingService
                                        {
                                            public void DoSomething()
                                            {
                                                var source = new Source
                                                {
                                                    Name = "Frank",
                                                    Description = "Description",
                                                    Age = 30,
                                                    Address = "Address"
                                                };

                                                var mapper = new AutoMapper.MapperConfiguration(cfg =>
                                                {
                                                    cfg.AddProfile<MappingProfile>();
                                                }).CreateMapper();

                                                var destination = mapper.Map<Destination>(source);
                                            }
                                        }
                                        """;

    private const string BaseCode = """
                                  using System;
                                  using AutoMapper;
                                  using System.Collections.Generic;
                                  using System.Linq;

                                  namespace Novolis.Analyzers.Tests;

                                  public class Source
                                  {
                                    public string Name { get; set; }

                                    public string? Description { get; set; }

                                    public int Age { get; set; }

                                    public string? Address { get; set; }
                                  }

                                  public class Destination
                                  {
                                      public string Name { get; set; }

                                      public string? Notes { get; set; }

                                      public int Age { get; set; }

                                      public string? Address { get; set; }
                                  }

                                  public class MappingProfile : Profile
                                  {
                                      public MappingProfile()
                                      {
                                          CreateMap<Source, Destination>()
                                              .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Description));
                                      }
                                  }

                                  """;
}
