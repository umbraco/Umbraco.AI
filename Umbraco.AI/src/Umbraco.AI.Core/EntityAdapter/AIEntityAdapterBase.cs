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
    /// <remarks>
    /// Declared here (rather than left to <see cref="IAIEntityAdapter"/>'s default interface
    /// implementation) so derived adapters get a real virtual slot to <c>override</c>. Without it,
    /// a derived class's same-signature method would not participate in the interface's dispatch
    /// table, and calls through an <see cref="IAIEntityAdapter"/>-typed reference — how
    /// <c>AIEntityContextHelper</c> always calls it, since <c>AIEntityAdapterCollection.GetAdapter</c>
    /// returns the interface — would silently fall through to the default implementation. Keep this
    /// <c>virtual</c>.
    /// </remarks>
    public virtual Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
        => Task.FromResult(FormatForLlm(entity));

    /// <inheritdoc />
    public virtual Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIEntitySubType>>([]);
}
