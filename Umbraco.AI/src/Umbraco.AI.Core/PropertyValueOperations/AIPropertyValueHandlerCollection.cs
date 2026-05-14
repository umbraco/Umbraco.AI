using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// A collection of property value handlers, resolved by the dispatcher per editor schema alias.
/// </summary>
/// <remarks>
/// Handlers are auto-discovered via <see cref="IDiscoverable"/>. Use
/// <see cref="AIPropertyValueHandlerCollectionBuilder"/> to add or exclude handlers in a Composer.
/// </remarks>
public sealed class AIPropertyValueHandlerCollection : BuilderCollectionBase<IAIPropertyValueHandler>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPropertyValueHandlerCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the handler instances.</param>
    public AIPropertyValueHandlerCollection(Func<IEnumerable<IAIPropertyValueHandler>> items)
        : base(items)
    { }

    /// <summary>
    /// Resolves the handler registered for a given property editor schema alias.
    /// </summary>
    /// <param name="propertyEditorSchemaAlias">The schema alias (e.g. <c>Umbraco.BlockList</c>).</param>
    /// <returns>The handler, or <c>null</c> if none is registered for the alias.</returns>
    public IAIPropertyValueHandler? GetByEditorSchemaAlias(string propertyEditorSchemaAlias)
        => this.FirstOrDefault(h =>
            string.Equals(h.ForPropertyEditorSchemaAlias, propertyEditorSchemaAlias, StringComparison.OrdinalIgnoreCase));
}
