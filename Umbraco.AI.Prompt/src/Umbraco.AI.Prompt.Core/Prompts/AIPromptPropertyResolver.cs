using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Resolves the <see cref="IPropertyType"/> for a content/media/member/block type alias and property alias.
/// Shared by <see cref="AIPromptScopeValidator"/> (editor UI alias lookup) and
/// <see cref="AIPromptPropertyValueSchemaResolver"/> (value schema lookup) so both resolve the
/// same property type the same way.
/// </summary>
internal static class AIPromptPropertyResolver
{
    /// <summary>
    /// Resolves the property type for the given content type alias, entity type, and property alias.
    /// </summary>
    public static IPropertyType? ResolvePropertyType(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        string contentTypeAlias,
        string entityType,
        string propertyAlias)
    {
        IContentTypeBase? contentType = entityType.ToLowerInvariant() switch
        {
            "document" or "block" => contentTypeService.Get(contentTypeAlias),
            "media" => mediaTypeService.Get(contentTypeAlias),
            "member" => memberTypeService.Get(contentTypeAlias),
            _ => contentTypeService.Get(contentTypeAlias), // Default fallback
        };

        return contentType is IContentTypeComposition compositionContentType
            ? compositionContentType.CompositionPropertyTypes.FirstOrDefault(
                pt => pt.Alias.Equals(propertyAlias, StringComparison.OrdinalIgnoreCase))
            : contentType?.PropertyTypes.FirstOrDefault(
                pt => pt.Alias.Equals(propertyAlias, StringComparison.OrdinalIgnoreCase));
    }
}
