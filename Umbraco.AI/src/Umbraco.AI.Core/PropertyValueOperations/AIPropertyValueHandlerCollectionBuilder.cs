using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// A lazy collection builder for property value handlers.
/// </summary>
/// <remarks>
/// Handlers are auto-discovered via <see cref="IDiscoverable"/> and the
/// <see cref="IAIPropertyValueHandler.ForPropertyEditorSchemaAlias"/> registration. Use
/// <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add handlers
/// manually, or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to
/// exclude auto-discovered handlers.
/// </remarks>
public class AIPropertyValueHandlerCollectionBuilder
    : LazyCollectionBuilderBase<AIPropertyValueHandlerCollectionBuilder, AIPropertyValueHandlerCollection, IAIPropertyValueHandler>
{
    /// <inheritdoc />
    protected override AIPropertyValueHandlerCollectionBuilder This => this;
}
