using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Resolves the default value for a property editor / data type. Used when a handler creates a new
/// item (<see cref="AIPropertyOperation.AddItem"/>) and needs to fill in property values the caller
/// did not supply.
/// </summary>
/// <remarks>
/// The implementation wraps Umbraco CMS's server-side default-value mechanisms
/// (<c>IDataValueEditor.DefaultValue</c>, <c>IPropertyType.DefaultValue</c>, etc.). Handlers
/// consume defaults through this abstraction so we have a single point of integration with CMS's
/// value-preset behaviour and a single point to replace if CMS later exposes a unified
/// <c>IPropertyValueDefaultProvider</c>.
/// </remarks>
public interface IAIPropertyDefaultValueProvider
{
    /// <summary>
    /// Returns the default JSON value for the given data type.
    /// </summary>
    /// <param name="dataTypeKey">The data type key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default value as a <see cref="JsonNode"/>, or <c>null</c> for editors whose default is empty.</returns>
    Task<JsonNode?> GetDefaultValueAsync(Guid dataTypeKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the default JSON values for every property on a content/element type, keyed by alias.
    /// </summary>
    /// <param name="contentTypeKey">The content (or element) type key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A mapping of property alias to default value.</returns>
    Task<IReadOnlyDictionary<string, JsonNode?>> GetDefaultValuesForContentTypeAsync(
        Guid contentTypeKey,
        CancellationToken cancellationToken = default);
}
