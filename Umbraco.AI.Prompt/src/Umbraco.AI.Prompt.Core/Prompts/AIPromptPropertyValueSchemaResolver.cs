using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <inheritdoc cref="IAIPromptPropertyValueSchemaResolver" />
internal sealed class AIPromptPropertyValueSchemaResolver : IAIPromptPropertyValueSchemaResolver
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;

    public AIPromptPropertyValueSchemaResolver(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        IPropertyEditorSchemaService propertyEditorSchemaService)
    {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
        _propertyEditorSchemaService = propertyEditorSchemaService;
    }

    /// <inheritdoc />
    public async Task<JsonObject?> ResolveValueSchemaAsync(
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

        var attempt = await _propertyEditorSchemaService.GetSchemaAsync(propertyType.DataTypeKey);

        return attempt.Success ? attempt.Result?.JsonSchema : null;
    }
}
