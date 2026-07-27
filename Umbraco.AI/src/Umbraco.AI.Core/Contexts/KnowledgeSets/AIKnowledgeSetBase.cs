using System.Reflection;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// Base class for AI knowledge sets that reads its <see cref="AIKnowledgeSetAttribute"/>
/// reflectively and exposes its metadata.
/// </summary>
/// <remarks>
/// Derive from this class, decorate it with <see cref="AIKnowledgeSetAttribute"/>, and override
/// <see cref="GetItemsAsync"/> to supply the knowledge items.
/// </remarks>
public abstract class AIKnowledgeSetBase : IAIKnowledgeSet
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public string? Icon { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIKnowledgeSetBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the knowledge set is missing the required attribute.</exception>
    protected AIKnowledgeSetBase()
    {
        var attribute = GetType().GetCustomAttribute<AIKnowledgeSetAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Knowledge set '{GetType().FullName}' is missing required [AIKnowledgeSet] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
        Description = attribute.Description;
        Icon = attribute.Icon;
    }

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default);
}
