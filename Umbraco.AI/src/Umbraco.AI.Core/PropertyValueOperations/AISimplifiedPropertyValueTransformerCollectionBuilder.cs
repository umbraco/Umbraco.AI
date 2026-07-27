using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// A lazy collection builder for simplified property value transformers.
/// </summary>
/// <remarks>
/// Transformers are auto-discovered via <see cref="IDiscoverable"/> and registered against an editor
/// schema alias (<see cref="IAISimplifiedPropertyValueTransformer.ForPropertyEditorSchemaAlias"/>). Use
/// <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add transformers
/// manually, or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to
/// exclude auto-discovered transformers.
/// </remarks>
public class AISimplifiedPropertyValueTransformerCollectionBuilder
    : LazyCollectionBuilderBase<AISimplifiedPropertyValueTransformerCollectionBuilder, AISimplifiedPropertyValueTransformerCollection, IAISimplifiedPropertyValueTransformer>
{
    /// <inheritdoc />
    protected override AISimplifiedPropertyValueTransformerCollectionBuilder This => this;
}
