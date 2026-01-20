using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Prompt.Core.Media;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Ai.Prompt.Core.Templates.Processors;

/// <summary>
/// Template variable processor that handles image content.
/// Handles prefixed variables like {{image:umbracoFile}} or {{image:mediaProperty}}.
/// Fetches the property value from the current entity using Umbraco's content/media services.
/// Returns the image followed by a reference name that the AI can use to identify it.
/// </summary>
internal sealed class ImageTemplateVariableProcessor : IAiTemplateVariableProcessor
{
    private readonly IMediaService _mediaService;
    private readonly IContentService _contentService;
    private readonly IAiMediaImageResolver _imageResolver;
    private readonly ILogger<ImageTemplateVariableProcessor> _logger;

    public ImageTemplateVariableProcessor(
        IMediaService mediaService,
        IContentService contentService,
        IAiMediaImageResolver imageResolver,
        ILogger<ImageTemplateVariableProcessor> logger)
    {
        _mediaService = mediaService;
        _contentService = contentService;
        _imageResolver = imageResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Prefix => "image";

    /// <inheritdoc />
    public IEnumerable<AIContent> Process(string path, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(context);

        // Get the entity ID and type from context
        if (!TryGetEntityInfo(context, out var entityId, out var entityType))
        {
            _logger.LogWarning("Cannot process image variable '{Path}': missing entityId or entityType in context", path);
            yield break;
        }

        // Fetch the entity
        IContentBase? entity = entityType.Equals("media", StringComparison.OrdinalIgnoreCase)
            ? _mediaService.GetById(entityId)
            : _contentService.GetById(entityId);

        if (entity is null)
        {
            _logger.LogWarning("Entity {EntityId} of type {EntityType} not found", entityId, entityType);
            yield break;
        }

        // Get the property value
        var propertyValue = entity.GetValue(path);
        if (propertyValue is null)
        {
            _logger.LogWarning("Cannot process image variable '{Path}': property not found on entity {EntityId}", path, entityId);
            yield break;
        }

        // Use the media resolver to get the image content
        var imageContent = _imageResolver.Resolve(propertyValue);
        if (imageContent is null)
        {
            _logger.LogWarning("Cannot process image variable '{Path}': failed to resolve image from property value", path);
            yield break;
        }

        // Return the image as DataContent
        yield return new DataContent(imageContent.Data, imageContent.MediaType);

        // Return a reference name that the AI can use to identify this image
        // Use the entity name if available, otherwise use the property alias
        var referenceName = !string.IsNullOrWhiteSpace(entity.Name)
            ? entity.Name
            : $"image_{path}";

        yield return new TextContent($" [Image: {referenceName}]");
    }

    private static bool TryGetEntityInfo(IReadOnlyDictionary<string, object?> context, out Guid entityId, out string entityType)
    {
        entityId = Guid.Empty;
        entityType = string.Empty;

        // Get entityId - could be Guid or string
        if (!context.TryGetValue("entityId", out var entityIdValue) || entityIdValue is null)
        {
            return false;
        }

        if (entityIdValue is Guid guid)
        {
            entityId = guid;
        }
        else if (entityIdValue is string idStr && Guid.TryParse(idStr, out var parsedGuid))
        {
            entityId = parsedGuid;
        }
        else
        {
            return false;
        }

        // Get entityType
        if (!context.TryGetValue("entityType", out var entityTypeValue) || entityTypeValue is not string typeStr)
        {
            return false;
        }

        entityType = typeStr;
        return true;
    }
}
