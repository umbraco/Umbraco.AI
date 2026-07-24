using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// A collection of simplified property value transformers, resolved per editor schema alias.
/// </summary>
/// <remarks>
/// Transformers are auto-discovered via <see cref="IDiscoverable"/>. Use
/// <see cref="AISimplifiedPropertyValueTransformerCollectionBuilder"/> to add or exclude transformers
/// in a Composer.
/// </remarks>
public sealed class AISimplifiedPropertyValueTransformerCollection
    : BuilderCollectionBase<IAISimplifiedPropertyValueTransformer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISimplifiedPropertyValueTransformerCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the transformer instances.</param>
    public AISimplifiedPropertyValueTransformerCollection(Func<IEnumerable<IAISimplifiedPropertyValueTransformer>> items)
        : base(items)
    { }

    /// <summary>
    /// Resolves the transformer registered for a given property editor schema alias.
    /// </summary>
    /// <param name="propertyEditorSchemaAlias">The schema alias (e.g. <c>Umbraco.RichText</c>).</param>
    /// <returns>The transformer, or <c>null</c> if none is registered for the alias.</returns>
    public IAISimplifiedPropertyValueTransformer? GetByEditorSchemaAlias(string propertyEditorSchemaAlias)
        => this.FirstOrDefault(t =>
            string.Equals(t.ForPropertyEditorSchemaAlias, propertyEditorSchemaAlias, StringComparison.OrdinalIgnoreCase));
}
