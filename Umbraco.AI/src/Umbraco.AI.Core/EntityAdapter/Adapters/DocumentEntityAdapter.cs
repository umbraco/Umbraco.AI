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

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentEntityAdapter"/> class.
    /// </summary>
    public DocumentEntityAdapter(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
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
        return CmsEntityFormatHelper.FormatCmsEntity(entity);
    }

    /// <inheritdoc />
    public override Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
    {
        var contentTypes = _contentTypeService.GetAll()
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
