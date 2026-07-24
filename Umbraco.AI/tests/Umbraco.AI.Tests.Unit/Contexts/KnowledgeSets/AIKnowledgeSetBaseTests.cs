using Umbraco.AI.Core.Contexts.KnowledgeSets;

namespace Umbraco.AI.Tests.Unit.Contexts.KnowledgeSets;

public class AIKnowledgeSetBaseTests
{
    [AIKnowledgeSet("test-set", "Test Set", Description = "A test set", Icon = "icon-book")]
    private sealed class DecoratedKnowledgeSet : AIKnowledgeSetBase
    {
        public override Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIKnowledgeSetItem>>([]);
    }

    private sealed class UndecoratedKnowledgeSet : AIKnowledgeSetBase
    {
        public override Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIKnowledgeSetItem>>([]);
    }

    [Fact]
    public void Ctor_ReadsMetadataFromAttribute()
    {
        var set = new DecoratedKnowledgeSet();

        set.Id.ShouldBe("test-set");
        set.Name.ShouldBe("Test Set");
        set.Description.ShouldBe("A test set");
        set.Icon.ShouldBe("icon-book");
    }

    [Fact]
    public void Ctor_MissingAttribute_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => new UndecoratedKnowledgeSet());

        ex.Message.ShouldContain("[AIKnowledgeSet]");
    }
}
