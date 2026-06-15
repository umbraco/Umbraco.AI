using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Adapter for Umbraco CMS document entities.
/// Provides property-based formatting and content type sub-types.
/// Falls back to generic JSON formatting if the data structure doesn't match.
/// </summary>
internal sealed class DocumentEntityAdapter : AIEntityAdapterBase
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentEntityAdapter"/> class.
    /// </summary>
    public DocumentEntityAdapter(
        IContentTypeService contentTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        IPropertyEditorSchemaService propertyEditorSchemaService)
    {
        _contentTypeService = contentTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertyEditorSchemaService = propertyEditorSchemaService;
    }

    /// <inheritdoc />
    public override string? EntityType => "document";

    /// <inheritdoc />
    public override string Name => "Document";

    /// <inheritdoc />
    public override string? Icon => "icon-document";

    /// <inheritdoc />
    public override bool HasSubTypes => true;

    /// <inheritdoc />
    public override string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return CmsEntityFormatHelper.FormatCmsEntity(
            entity,
            _publishedContentTypeCache,
            _propertyEditorSchemaService,
            PublishedItemType.Content);
    }

    /// <inheritdoc />
    public override Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
    {
        var contentTypes = _contentTypeService.GetAll()
            .Where(x => x is { IsElement: false, AllowedTemplates: not null } && x.AllowedTemplates.Any()) // Only include content types that can be created (not elements) and have templates (i.e., are not purely structural)
            .Select(ct => new AIEntitySubType
            {
                Alias = ct.Alias,
                Name = ct.Name ?? ct.Alias,
                Icon = ct.Icon,
                Description = ct.Description,
                Unique = ct.Key.ToString()
            })
            .OrderBy(ct => ct.Name);

        return Task.FromResult<IEnumerable<AIEntitySubType>>(contentTypes);
    }
}
