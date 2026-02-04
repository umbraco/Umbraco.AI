using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// A lazy collection builder for AI test graders.
/// </summary>
/// <remarks>
/// Graders are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AITestGraderAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add graders manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered graders.
/// </remarks>
public class AITestGraderCollectionBuilder
    : LazyCollectionBuilderBase<AITestGraderCollectionBuilder, AITestGraderCollection, IAITestGrader>
{
    /// <inheritdoc />
    protected override AITestGraderCollectionBuilder This => this;
}
