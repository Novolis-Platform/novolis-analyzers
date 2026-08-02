using Microsoft.CodeAnalysis;
using Novolis.Analyzers.CodeLength.Internals;
using TUnit.Core;

namespace Novolis.Analyzers.Tests.Analyzers;

public sealed class CodeLengthInternalsTests
{
    [Test]
    public async Task DiagnosticIdBuilder_WithCustomPrefix_BuildsExpectedId()
    {
        var id = new DiagnosticIdBuilder()
            .WithPrefix("custom")
            .WithCategory(DiagnosticCategories.Maintainability)
            .WithId(7)
            .Build();

        await Assert.That(id).IsEqualTo("CUSTOM4007");
    }

    [Test]
    public async Task DiagnosticIdBuilder_WithoutCategory_Throws()
    {
        await Assert.That(() => new DiagnosticIdBuilder().WithId(1).Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DiagnosticIdBuilder_WithEmptyPrefix_Throws()
    {
        await Assert.That(() => new DiagnosticIdBuilder()
                .WithPrefix(string.Empty)
                .WithCategory(DiagnosticCategories.Usage)
                .WithId(1)
                .Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DiagnosticIdBuilder_WithZeroId_Throws()
    {
        await Assert.That(() => new DiagnosticIdBuilder()
                .WithCategory(DiagnosticCategories.Usage)
                .WithId(0)
                .Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DiagnosticIdBuilder_WithIdAbove999_Throws()
    {
        await Assert.That(() => new DiagnosticIdBuilder()
                .WithCategory(DiagnosticCategories.Usage)
                .WithId(1000)
                .Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DiagnosticDescriptorBuilder_WithoutRequiredFields_Throws()
    {
        await Assert.That(() => new DiagnosticDescriptorBuilder().Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticDescriptorBuilder()
                .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Usage).WithId(1))
                .Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticDescriptorBuilder()
                .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Usage).WithId(1))
                .WithTitle("Title")
                .Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticDescriptorBuilder()
                .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Usage).WithId(1))
                .WithTitle("Title")
                .WithMessageFormat("Message {0}")
                .Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticDescriptorBuilder()
                .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Usage).WithId(1))
                .WithTitle("Title")
                .WithMessageFormat("Message {0}")
                .WithDescription("Description")
                .Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DiagnosticBuilder_WithoutRequiredFields_Throws()
    {
        var descriptor = new DiagnosticDescriptorBuilder()
            .WithIdBuilder(new DiagnosticIdBuilder().WithCategory(DiagnosticCategories.Usage).WithId(1))
            .WithTitle("Title")
            .WithMessageFormat("Message {0} {1}")
            .WithCategory(DiagnosticCategories.Usage)
            .WithDescription("Description")
            .Build();

        await Assert.That(() => new DiagnosticBuilder().Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticBuilder().WithDescriptor(descriptor).Build())
            .Throws<InvalidOperationException>();

        await Assert.That(() => new DiagnosticBuilder()
                .WithDescriptor(descriptor)
                .WithLocation(Location.None)
                .WithArguments("only-one")
                .Build())
            .Throws<InvalidOperationException>();
    }
}
