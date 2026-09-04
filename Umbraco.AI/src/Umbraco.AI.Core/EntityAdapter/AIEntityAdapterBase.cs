namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Base class for entity adapters providing sensible defaults.
/// </summary>
public abstract class AIEntityAdapterBase : IAIEntityAdapter
{
    /// <inheritdoc />
    public abstract string? EntityType { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual string? Icon => null;

    /// <inheritdoc />
    public virtual bool HasSubTypes => false;

    /// <inheritdoc />
    public abstract string FormatForLlm(AISerializedEntity entity);

    /// <inheritdoc />
    public virtual Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIEntitySubType>>([]);
}
