using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Prompt.Core.Media;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Templates.Processors;

/// <summary>
/// Template variable processor that handles image content.
/// Handles prefixed variables like <c>{{image:umbracoFile}}</c> or
/// <c>{{image:umbracoFile#cropAlias}}</c>. Everything after the first
/// <c>#</c> in the path is treated as an image cropper crop alias and
/// applied via <see cref="IAIUmbracoMediaResolver"/> before the image
/// is sent to the AI provider.
/// Fetches the property value from the current entity using Umbraco's content/media services.
/// Returns the image followed by a reference name that the AI can use to identify it.
/// </summary>
internal sealed class ImageTemplateVariableProcessor : IAITemplateVariableProcessor
{
    private readonly IMediaService _mediaService;
    private readonly IContentService _contentService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly ILogger<ImageTemplateVariableProcessor> _logger;

    public ImageTemplateVariableProcessor(
        IMediaService mediaService,
        IContentService contentService,
        IAIUmbracoMediaResolver mediaResolver,
        ILogger<ImageTemplateVariableProcessor> logger)
    {
        _mediaService = mediaService;
        _contentService = contentService;
        _mediaResolver = mediaResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Prefix => "image";

    /// <inheritdoc />
    public async Task<IEnumerable<AIContent>>ProcessAsync(string path, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<AIContent>();

        // Split the path on '#' to separate the property alias from an optional crop alias.
        // Property aliases are identifier-safe and can never contain '#', so the first '#'
        // cleanly marks the crop suffix.
        var (propertyAlias, cropAlias) = SplitPath(path);

        // Get the entity ID and type from context
        if (!TryGetEntityInfo(context, out var entityId, out var entityType))
        {
            _logger.LogWarning("Cannot process image variable '{Path}': missing entityId or entityType in context", path);
            return results;
        }

        // Fetch the entity
        IContentBase? entity = entityType.Equals("media", StringComparison.OrdinalIgnoreCase)
            ? _mediaService.GetById(entityId)
            : _contentService.GetById(entityId);

        if (entity is null)
        {
            _logger.LogWarning("Entity {EntityId} of type {EntityType} not found", entityId, entityType);
            return results;
        }

        // Get the property value
        var propertyValue = entity.GetValue(propertyAlias);
        if (propertyValue is null)
        {
            _logger.LogWarning("Cannot process image variable '{Path}': property not found on entity {EntityId}", path, entityId);
            return results;
        }

        // Use the media resolver to get the image content, applying the crop when specified.
        var imageContent = await _mediaResolver.ResolveAsync(propertyValue, cropAlias, cancellationToken);
        if (imageContent is null)
        {
            _logger.LogWarning("Cannot process image variable '{Path}': failed to resolve image from property value", path);
            return results;
        }

        // Return the image as DataContent
        results.Add(new DataContent(imageContent.Data, imageContent.MediaType));

        // Return a reference name that the AI can use to identify this image.
        // When a crop was requested, include the crop alias so the AI can distinguish
        // multiple crops of the same source image in a single prompt.
        var baseName = !string.IsNullOrWhiteSpace(entity.Name)
            ? entity.Name
            : $"image_{propertyAlias}";

        var referenceName = string.IsNullOrWhiteSpace(cropAlias)
            ? baseName
            : $"{baseName} ({cropAlias})";

        results.Add(new TextContent($" [Image: {referenceName}]"));

        return results;
    }

    private static (string PropertyAlias, string? CropAlias) SplitPath(string path)
    {
        var hashIndex = path.IndexOf('#');
        if (hashIndex < 0)
        {
            return (path, null);
        }

        var propertyAlias = path[..hashIndex];
        var cropAlias = path[(hashIndex + 1)..];

        return (
            propertyAlias,
            string.IsNullOrWhiteSpace(cropAlias) ? null : cropAlias);
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
