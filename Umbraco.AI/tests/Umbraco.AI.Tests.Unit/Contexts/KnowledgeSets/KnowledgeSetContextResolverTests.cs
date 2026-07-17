using Microsoft.Extensions.Configuration;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Contexts.KnowledgeSets;

public class KnowledgeSetContextResolverTests
{
    private static AIEditableModelResolver CreateModelResolver()
        => new(new ConfigurationBuilder().Build());

    private static AIKnowledgeSetCollection CreateCollection(params IAIKnowledgeSet[] sets)
        => new(() => sets);

    private static KnowledgeSetContextResolver CreateResolver(params IAIKnowledgeSet[] sets)
        => new(CreateCollection(sets), CreateModelResolver());

    [Fact]
    public async Task ResolveAsync_NoKnowledgeSets_ReturnsEmpty()
    {
        var result = await CreateResolver().ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_SetWithNoItems_IsSkipped()
    {
        var set = new FakeKnowledgeSet(items: []);

        var result = await CreateResolver(set).ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_MapsItemsToOnDemandTextResources()
    {
        var set = new FakeKnowledgeSet(
            id: "engage",
            name: "Umbraco Engage",
            description: "Background knowledge about Umbraco Engage",
            items:
            [
                new AIKnowledgeSetItem { Name = "Goals", Description = "How goals work", Content = "# Goals\nDetails" },
                new AIKnowledgeSetItem { Name = "Segments", Content = "# Segments" }
            ]);

        var result = await CreateResolver(set).ResolveAsync();

        result.Resources.Count.ShouldBe(2);

        var goals = result.Resources.Single(r => r.Name == "Goals");
        goals.ResourceTypeId.ShouldBe("text");
        goals.InjectionMode.ShouldBe(AIContextResourceInjectionMode.OnDemand);
        goals.Description.ShouldBe("How goals work");
        goals.ContextName.ShouldBe("Umbraco Engage");
        goals.ContextDescription.ShouldBe("Background knowledge about Umbraco Engage");
        goals.Settings.ShouldBeOfType<TextResourceSettings>().Content.ShouldBe("# Goals\nDetails");

        // One source per set, tagged with the set name as the context name.
        result.Sources.ShouldHaveSingleItem().ContextName.ShouldBe("Umbraco Engage");
    }

    [Fact]
    public async Task ResolveAsync_GuidsAreDeterministicAcrossResolvesAndNamespaced()
    {
        var items = new AIKnowledgeSetItem[]
        {
            new() { Name = "Goals", Content = "a" },
            new() { Name = "Segments", Content = "b" }
        };

        var first = await CreateResolver(new FakeKnowledgeSet(id: "engage", items: items)).ResolveAsync();
        var second = await CreateResolver(new FakeKnowledgeSet(id: "engage", items: items)).ResolveAsync();

        var firstIds = first.Resources.Select(r => r.Id).ToArray();
        var secondIds = second.Resources.Select(r => r.Id).ToArray();

        // Stable across independent resolves.
        firstIds.ShouldBe(secondIds);
        // Distinct per item.
        firstIds.Distinct().Count().ShouldBe(2);
        // Namespaced deterministic identifiers produced by the shared DeterministicGuid helper,
        // keyed on "{setId}\0{itemName}". Asserting equality with the helper both proves the values
        // are namespaced (never Guid.Empty) and pins the derivation to the reusable utility.
        firstIds[0].ShouldBe(KnowledgeSetContextResolver.CreateResourceId("engage", "Goals"));
        firstIds[1].ShouldBe(KnowledgeSetContextResolver.CreateResourceId("engage", "Segments"));
        firstIds.ShouldNotContain(Guid.Empty);
    }

    [Fact]
    public async Task ResolveAsync_DifferentSetsProduceDifferentGuidsForSameItemName()
    {
        var itemName = "Goals";
        var a = await CreateResolver(new FakeKnowledgeSet(id: "engage",
            items: [new AIKnowledgeSetItem { Name = itemName, Content = "a" }])).ResolveAsync();
        var b = await CreateResolver(new FakeKnowledgeSet(id: "commerce",
            items: [new AIKnowledgeSetItem { Name = itemName, Content = "b" }])).ResolveAsync();

        a.Resources.Single().Id.ShouldNotBe(b.Resources.Single().Id);
    }

    [Fact]
    public async Task ResolveAsync_ContentStartingWithDollar_IsEscapedAndSurvivesProcessorRoundTrip()
    {
        const string literal = "$5 per month";
        var set = new FakeKnowledgeSet(id: "pricing",
            items: [new AIKnowledgeSetItem { Name = "Pricing", Content = literal }]);

        var result = await CreateResolver(set).ResolveAsync();

        var resource = result.Resources.Single();
        // The stored settings content is escaped ($$) so it is never treated as a config reference.
        resource.Settings.ShouldBeOfType<TextResourceSettings>().Content.ShouldBe("$" + literal);

        // Running it through the same processing path the LLM uses returns the original literal.
        var processed = await CreateProcessor().ProcessResourceForLlmAsync(ToResolvedResource(resource));
        processed.ShouldBe(literal);
    }

    private static AIContextProcessor CreateProcessor()
    {
        var infrastructure = new AIContextResourceTypeInfrastructure(
            Mock.Of<IAIEditableModelSchemaBuilder>(),
            CreateModelResolver());
        var resourceTypes = new AIContextResourceTypeCollection(
            () => [new TextResourceType(infrastructure)]);
        return new AIContextProcessor(resourceTypes);
    }

    private static AIResolvedResource ToResolvedResource(
        Umbraco.AI.Core.Contexts.Resolvers.AIContextResolverResource resource)
        => new()
        {
            Id = resource.Id,
            ResourceTypeId = resource.ResourceTypeId,
            Name = resource.Name,
            Description = resource.Description,
            Settings = resource.Settings,
            InjectionMode = resource.InjectionMode,
            Source = nameof(KnowledgeSetContextResolver),
            ContextName = resource.ContextName
        };
}
