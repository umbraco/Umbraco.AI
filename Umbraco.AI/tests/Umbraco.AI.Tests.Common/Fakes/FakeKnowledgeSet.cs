using Umbraco.AI.Core.Contexts.KnowledgeSets;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIKnowledgeSet"/> for use in tests, yielding a configurable
/// list of items.
/// </summary>
public sealed class FakeKnowledgeSet : IAIKnowledgeSet
{
    private readonly IReadOnlyList<AIKnowledgeSetItem> _items;

    public FakeKnowledgeSet(
        string id = "fake-knowledge-set",
        string name = "Fake Knowledge Set",
        string? description = "A fake knowledge set for testing",
        string? icon = "icon-book",
        IReadOnlyList<AIKnowledgeSetItem>? items = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Icon = icon;
        _items = items ?? [];
    }

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Icon { get; }

    public Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_items);
}
