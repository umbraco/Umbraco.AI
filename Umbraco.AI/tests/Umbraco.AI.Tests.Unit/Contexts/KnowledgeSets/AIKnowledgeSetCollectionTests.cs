using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Contexts.KnowledgeSets;

public class AIKnowledgeSetCollectionTests
{
    private static AIKnowledgeSetCollection CreateCollection(params IAIKnowledgeSet[] sets)
        => new(() => sets);

    [Fact]
    public void GetById_ReturnsMatchingSet()
    {
        var engage = new FakeKnowledgeSet(id: "engage", name: "Umbraco Engage");
        var commerce = new FakeKnowledgeSet(id: "commerce", name: "Umbraco Commerce");
        var collection = CreateCollection(engage, commerce);

        collection.GetById("engage").ShouldBeSameAs(engage);
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        var engage = new FakeKnowledgeSet(id: "engage");
        var collection = CreateCollection(engage);

        collection.GetById("ENGAGE").ShouldBeSameAs(engage);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var collection = CreateCollection(new FakeKnowledgeSet(id: "engage"));

        collection.GetById("missing").ShouldBeNull();
    }
}
