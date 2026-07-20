using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Contexts.KnowledgeSets;

public class KnowledgeContentResourceTypeTests
{
    private static KnowledgeContentResourceType CreateResourceType(params IAIKnowledgeSet[] sets)
        => new(new AIKnowledgeSetCollection(() => sets), NullLogger<KnowledgeContentResourceType>.Instance);

    private static KnowledgeContentRef Ref(string setId, string itemKey)
        => new() { KnowledgeSetId = setId, ItemKey = itemKey };

    [Fact]
    public void Id_IsKnowledgeContent()
    {
        CreateResourceType().Id.ShouldBe("knowledge-content");
        KnowledgeContentResourceType.TypeId.ShouldBe("knowledge-content");
    }

    [Fact]
    public async Task ResolveDataAsync_AwaitsGetContentAsync_ReturnsMarkdown()
    {
        var set = new FakeKnowledgeSet(id: "engage",
            items: [AIKnowledgeSetItem.FromContent("goals", "Goals", "# Goals\nDetails")]);
        var resourceType = CreateResourceType(set);

        var data = await resourceType.ResolveDataAsync(Ref("engage", "goals"));

        resourceType.FormatDataForLlm(data).ShouldBe("# Goals\nDetails");
    }

    [Fact]
    public async Task ResolveDataAsync_ContentStartingWithDollar_IsReturnedLiterally()
    {
        // No config resolution runs for knowledge content — a leading '$' is treated literally.
        const string literal = "$5 per month";
        var set = new FakeKnowledgeSet(id: "pricing",
            items: [AIKnowledgeSetItem.FromContent("pricing", "Pricing", literal)]);
        var resourceType = CreateResourceType(set);

        var data = await resourceType.ResolveDataAsync(Ref("pricing", "pricing"));

        resourceType.FormatDataForLlm(data).ShouldBe(literal);
    }

    [Fact]
    public async Task ResolveDataAsync_UnknownSet_DegradesToEmpty()
    {
        var resourceType = CreateResourceType(new FakeKnowledgeSet(id: "engage"));

        var data = await resourceType.ResolveDataAsync(Ref("does-not-exist", "goals"));

        resourceType.FormatDataForLlm(data).ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveDataAsync_UnknownItem_DegradesToEmpty()
    {
        var set = new FakeKnowledgeSet(id: "engage",
            items: [AIKnowledgeSetItem.FromContent("goals", "Goals", "content")]);
        var resourceType = CreateResourceType(set);

        var data = await resourceType.ResolveDataAsync(Ref("engage", "missing"));

        resourceType.FormatDataForLlm(data).ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveDataAsync_ThrowingProducer_DegradesToEmpty()
    {
        var set = new FakeKnowledgeSet(id: "engage",
            items: [FakeKnowledgeSet.ThrowingItem("goals", "Goals")]);
        var resourceType = CreateResourceType(set);

        var data = await resourceType.ResolveDataAsync(Ref("engage", "goals"));

        resourceType.FormatDataForLlm(data).ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveDataAsync_NullSettings_DegradesToEmpty()
    {
        var resourceType = CreateResourceType(new FakeKnowledgeSet(id: "engage"));

        var data = await resourceType.ResolveDataAsync(null);

        resourceType.FormatDataForLlm(data).ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveDataAsync_PassesCancellationTokenToProducer()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;

        var item = new AIKnowledgeSetItem
        {
            Key = "goals",
            Name = "Goals",
            GetContentAsync = ct =>
            {
                received = ct;
                return Task.FromResult("content");
            }
        };
        var resourceType = CreateResourceType(new FakeKnowledgeSet(id: "engage", items: [item]));

        await resourceType.ResolveDataAsync(Ref("engage", "goals"), cts.Token);

        received.ShouldBe(cts.Token);
    }
}
