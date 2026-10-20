using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <inheritdoc cref="IAIPromptPropertyValueSchemaResolver" />
internal sealed class AIPromptPropertyValueSchemaResolver : IAIPromptPropertyValueSchemaResolver
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly AISimplifiedPropertyValueTransformerCollection _transformers;

    public AIPromptPropertyValueSchemaResolver(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        IPropertyEditorSchemaService propertyEditorSchemaService,
        AISimplifiedPropertyValueTransformerCollection transformers)
    {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _transformers = transformers;
    }

    /// <inheritdoc />
    [Obsolete("Use ResolvePropertyValueSchemaAsync. Will be removed in v20.")]
    public async Task<JsonObject?> ResolveValueSchemaAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolvePropertyValueSchemaAsync(contentTypeAlias, entityType, propertyAlias, cancellationToken);
        return resolution?.Schema;
    }

    /// <inheritdoc />
    public async Task<AIPromptPropertyValueSchemaResolution?> ResolvePropertyValueSchemaAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentTypeAlias) || string.IsNullOrEmpty(propertyAlias))
        {
            return null;
        }

        var propertyType = AIPromptPropertyResolver.ResolvePropertyType(
            _contentTypeService, _mediaTypeService, _memberTypeService,
            contentTypeAlias, entityType, propertyAlias);

        if (propertyType is null)
        {
            return null;
        }

        var dataTypeKey = propertyType.DataTypeKey;
        var editorAlias = propertyType.PropertyEditorAlias;

        // A registered transformer wins (always, when present) — it defines the simplified schema the
        // LLM should generate against, even for editors whose write schema is representable.
        var transformer = _transformers.GetByEditorSchemaAlias(editorAlias);
        if (transformer is not null)
        {
            var simplified = await transformer.GetSimplifiedSchemaAsync(dataTypeKey, cancellationToken);

            // Guard the JsonObject conversion: a non-object schema node (or an opt-out returning null)
            // falls through to the editor's write schema rather than throwing.
            if (simplified is JsonObject simplifiedObject)
            {
                return new AIPromptPropertyValueSchemaResolution(simplifiedObject, IsSimplified: true, dataTypeKey, editorAlias);
            }
        }

        var attempt = await _propertyEditorSchemaService.GetSchemaAsync(dataTypeKey);
        var writeSchema = attempt.Success ? attempt.Result?.JsonSchema : null;
        return new AIPromptPropertyValueSchemaResolution(writeSchema, IsSimplified: false, dataTypeKey, editorAlias);
    }
}
