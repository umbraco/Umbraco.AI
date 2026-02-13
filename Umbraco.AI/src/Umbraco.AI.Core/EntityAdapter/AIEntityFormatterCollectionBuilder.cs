using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Builder for the <see cref="AIEntityFormatterCollection"/>.
/// </summary>
public sealed class AIEntityFormatterCollectionBuilder
    : LazyCollectionBuilderBase<AIEntityFormatterCollectionBuilder, AIEntityFormatterCollection, IAIEntityFormatter>
{
    /// <inheritdoc />
    protected override AIEntityFormatterCollectionBuilder This => this;
}
