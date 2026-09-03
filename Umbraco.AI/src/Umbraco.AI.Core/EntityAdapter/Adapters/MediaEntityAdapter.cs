using Umbraco.AI.Core.FileProcessing;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Adapter for Umbraco CMS media entities.
/// Delegates formatting to the same CMS property-based logic as documents.
/// Provides media type sub-types.
/// </summary>
/// <remarks>
/// Redeclares <see cref="IAIEntityAdapter"/> (already implemented by <see cref="AIEntityAdapterBase"/>)
/// so this class's plain (non-<c>override</c>) <see cref="FormatForLlmAsync"/> participates in the
/// interface's dispatch table for this type. <see cref="AIEntityAdapterBase"/> never declares a class
/// member for <see cref="IAIEntityAdapter.FormatForLlmAsync"/> — it satisfies that member purely via
/// the interface's default implementation (C# 8 default interface method), so there is no virtual
/// slot to <c>override</c> here. Without this redeclaration, calls made through an
/// <see cref="IAIEntityAdapter"/>-typed reference (as <c>AIEntityContextHelper</c> does, since
/// <c>AIEntityAdapterCollection.GetAdapter</c> returns <see cref="IAIEntityAdapter"/>) would silently
/// resolve to the interface's default method instead of this override, even though a same-signature
/// concrete-type call would appear to work correctly. Do not remove this without re-verifying
/// interface-typed dispatch.
/// </remarks>
internal sealed class MediaEntityAdapter : AIEntityAdapterBase, IAIEntityAdapter
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly AIFileProcessingHandlerCollection _fileProcessingHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaEntityAdapter"/> class.
    /// </summary>
    public MediaEntityAdapter(
        IMediaTypeService mediaTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        IPropertyEditorSchemaService propertyEditorSchemaService,
        IAIUmbracoMediaResolver mediaResolver,
        AIFileProcessingHandlerCollection fileProcessingHandlers)
    {
        _mediaTypeService = mediaTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _mediaResolver = mediaResolver;
        _fileProcessingHandlers = fileProcessingHandlers;
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
        return CmsEntityFormatHelper.FormatCmsEntity(
            entity,
            _publishedContentTypeCache,
            _propertyEditorSchemaService,
            PublishedItemType.Media);
    }

    /// <summary>
    /// Formats a media entity for LLM consumption, appending extracted file text when the
    /// underlying file is a supported format — reusing the same file-processing handler
    /// pipeline that already services chat attachments (see
    /// <c>Umbraco.AI.Core.FileProcessing.AIFileProcessingChatClient</c>). Falls back to the
    /// metadata-only format from <see cref="FormatForLlm"/> when the media can't be resolved or
    /// no handler matches its MIME type.
    /// </summary>
    public async Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var baseline = FormatForLlm(entity);

        var media = await _mediaResolver.ResolveAsync(entity.Unique, cancellationToken: cancellationToken);
        if (media is null)
        {
            return baseline;
        }

        IAIFileProcessingHandler? handler = null;
        foreach (var candidate in _fileProcessingHandlers)
        {
            if (await candidate.CanHandleAsync(media.MediaType, cancellationToken))
            {
                handler = candidate;
                break;
            }
        }

        if (handler is null)
        {
            return baseline;
        }

        var result = await handler.ProcessAsync(media.Data, media.MediaType, entity.Name, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.Content))
        {
            return baseline;
        }

        return $"{baseline}\n\n{result.Content}";
    }

    /// <inheritdoc />
    public override Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
    {
        var mediaTypes = _mediaTypeService.GetAll()
            .Where(x => !x.IsElement)
            .Select(mt => new AIEntitySubType
            {
                Alias = mt.Alias,
                Name = mt.Name ?? mt.Alias,
                Icon = mt.Icon,
                Description = mt.Description,
                Unique = mt.Key.ToString()
            })
            .OrderBy(mt => mt.Name);

        return Task.FromResult<IEnumerable<AIEntitySubType>>(mediaTypes);
    }
}
