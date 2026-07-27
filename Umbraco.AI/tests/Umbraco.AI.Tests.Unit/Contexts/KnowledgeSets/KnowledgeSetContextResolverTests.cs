using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Contexts.KnowledgeSets;

public class KnowledgeSetContextResolverTests
{
    private static AIKnowledgeSetCollection CreateCollection(params IAIKnowledgeSet[] sets)
        => new(() => sets);

    private static KnowledgeSetContextResolver CreateResolver(params IAIKnowledgeSet[] sets)
        => new(CreateCollection(sets));

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
    public async Task ResolveAsync_MapsItemsToOnDemandKnowledgeContentReferences()
    {
        var set = new FakeKnowledgeSet(
            id: "engage",
            name: "Umbraco Engage",
            description: "Background knowledge about Umbraco Engage",
            items:
            [
                AIKnowledgeSetItem.FromContent("goals", "Goals", "# Goals\nDetails", "How goals work"),
                AIKnowledgeSetItem.FromContent("segments", "Segments", "# Segments")
            ]);

        var result = await CreateResolver(set).ResolveAsync();

        result.Resources.Count.ShouldBe(2);

        var goals = result.Resources.Single(r => r.Name == "Goals");
        goals.ResourceTypeId.ShouldBe("knowledge-content");
        goals.InjectionMode.ShouldBe(AIContextResourceInjectionMode.OnDemand);
        goals.Description.ShouldBe("How goals work");
        goals.ContextName.ShouldBe("Umbraco Engage");
        goals.ContextDescription.ShouldBe("Background knowledge about Umbraco Engage");

        // The resource carries a reference to the item, never the content itself.
        var reference = goals.Settings.ShouldBeOfType<KnowledgeContentRef>();
        reference.KnowledgeSetId.ShouldBe("engage");
        reference.ItemKey.ShouldBe("goals");

        // One source per set, tagged with the set name as the context name.
        result.Sources.ShouldHaveSingleItem().ContextName.ShouldBe("Umbraco Engage");
    }

    [Fact]
    public async Task ResolveAsync_GuidsAreDeterministicAcrossResolvesAndNamespaced()
    {
        var items = new[]
        {
            AIKnowledgeSetItem.FromContent("goals", "Goals", "a"),
            AIKnowledgeSetItem.FromContent("segments", "Segments", "b")
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
        // keyed on "{setId}\0{itemKey}". Asserting equality with the helper both proves the values
        // are namespaced (never Guid.Empty) and pins the derivation to the reusable utility.
        firstIds[0].ShouldBe(KnowledgeSetContextResolver.CreateResourceId("engage", "goals"));
        firstIds[1].ShouldBe(KnowledgeSetContextResolver.CreateResourceId("engage", "segments"));
        firstIds.ShouldNotContain(Guid.Empty);
    }

    [Fact]
    public async Task ResolveAsync_GuidDerivesFromKeyNotName_StableAcrossRename()
    {
        var beforeRename = await CreateResolver(new FakeKnowledgeSet(id: "engage",
            items: [AIKnowledgeSetItem.FromContent("goals", "Goals", "a")])).ResolveAsync();
        var afterRename = await CreateResolver(new FakeKnowledgeSet(id: "engage",
            items: [AIKnowledgeSetItem.FromContent("goals", "Conversion Goals", "a")])).ResolveAsync();

        // The display name changed but the key did not, so the GUID is unchanged.
        afterRename.Resources.Single().Id.ShouldBe(beforeRename.Resources.Single().Id);
    }

    [Fact]
    public async Task ResolveAsync_DifferentSetsProduceDifferentGuidsForSameItemKey()
    {
        var a = await CreateResolver(new FakeKnowledgeSet(id: "engage",
            items: [AIKnowledgeSetItem.FromContent("goals", "Goals", "a")])).ResolveAsync();
        var b = await CreateResolver(new FakeKnowledgeSet(id: "commerce",
            items: [AIKnowledgeSetItem.FromContent("goals", "Goals", "b")])).ResolveAsync();

        a.Resources.Single().Id.ShouldNotBe(b.Resources.Single().Id);
    }
}
