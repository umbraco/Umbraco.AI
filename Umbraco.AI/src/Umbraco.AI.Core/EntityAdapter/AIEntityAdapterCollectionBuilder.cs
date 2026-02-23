using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Builder for the <see cref="AIEntityAdapterCollection"/>.
/// </summary>
public sealed class AIEntityAdapterCollectionBuilder
    : LazyCollectionBuilderBase<AIEntityAdapterCollectionBuilder, AIEntityAdapterCollection, IAIEntityAdapter>
{
    /// <inheritdoc />
    protected override AIEntityAdapterCollectionBuilder This => this;
}
