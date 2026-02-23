using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Adapter for Umbraco CMS media entities.
/// Delegates formatting to the same CMS property-based logic as documents.
/// Provides media type sub-types.
/// </summary>
internal sealed class MediaEntityAdapter : AIEntityAdapterBase
{
    private readonly IMediaTypeService _mediaTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaEntityAdapter"/> class.
    /// </summary>
    public MediaEntityAdapter(IMediaTypeService mediaTypeService)
    {
        _mediaTypeService = mediaTypeService;
    }

    /// <inheritdoc />
    public override string? EntityType => "media";

    /// <inheritdoc />
    public override string Name => "Media";

    /// <inheritdoc />
    public override string? Icon => "icon-picture";

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
        var mediaTypes = _mediaTypeService.GetAll()
            .Select(mt => new AIEntitySubType
            {
                Alias = mt.Alias,
                Name = mt.Name ?? mt.Alias,
                Icon = mt.Icon,
                Description = mt.Description
            })
            .OrderBy(mt => mt.Name);

        return Task.FromResult<IEnumerable<AIEntitySubType>>(mediaTypes);
    }
}
