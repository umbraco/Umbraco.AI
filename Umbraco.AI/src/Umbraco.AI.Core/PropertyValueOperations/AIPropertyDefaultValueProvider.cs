using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Default <see cref="IAIPropertyDefaultValueProvider"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Umbraco CMS does not currently expose a uniform server-side equivalent of the frontend
/// <c>propertyValuePreset</c> extension type — defaults are scattered across editor implementations,
/// data-type configurations, and content-type metadata, with no single discovery surface. Until
/// CMS provides a unified <c>IPropertyValueDefaultProvider</c> (filed as a follow-up CMS proposal),
/// this implementation returns <c>null</c> for every lookup.
/// </para>
/// <para>
/// In practice this is acceptable for v1: handlers expose their own canonical empty representations
/// (e.g. block-list returns an empty envelope from <c>ClearAsync</c>); scalar editors default to
/// <c>null</c>; the LLM's "create with empty defaults, then fill via subsequent calls" pattern
/// covers the gaps explicitly through tool calls rather than implicit defaults.
/// </para>
/// </remarks>
public sealed class AIPropertyDefaultValueProvider : IAIPropertyDefaultValueProvider
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;

    /// <summary>
    /// Initializes a new <see cref="AIPropertyDefaultValueProvider"/>.
    /// </summary>
    public AIPropertyDefaultValueProvider(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService)
    {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
    }

    /// <inheritdoc />
    public Task<JsonNode?> GetDefaultValueAsync(Guid dataTypeKey, CancellationToken cancellationToken = default)
        => Task.FromResult<JsonNode?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, JsonNode?>> GetDefaultValuesForContentTypeAsync(
        Guid contentTypeKey,
        CancellationToken cancellationToken = default)
    {
        var contentType = (IContentTypeComposition?)_contentTypeService.Get(contentTypeKey)
            ?? _mediaTypeService.Get(contentTypeKey);

        var result = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        if (contentType is null)
        {
            return Task.FromResult<IReadOnlyDictionary<string, JsonNode?>>(result);
        }

        foreach (var property in contentType.CompositionPropertyTypes)
        {
            // Always null for now; handlers supply editor-specific empty values where appropriate.
            result[property.Alias] = null;
        }

        return Task.FromResult<IReadOnlyDictionary<string, JsonNode?>>(result);
    }
}
