using Microsoft.Extensions.Logging;
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
internal sealed class MediaEntityAdapter : AIEntityAdapterBase
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly AIFileProcessingHandlerCollection _fileProcessingHandlers;
    private readonly ILogger<MediaEntityAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaEntityAdapter"/> class.
    /// </summary>
    public MediaEntityAdapter(
        IMediaTypeService mediaTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        IPropertyEditorSchemaService propertyEditorSchemaService,
        IAIUmbracoMediaResolver mediaResolver,
        AIFileProcessingHandlerCollection fileProcessingHandlers,
        ILogger<MediaEntityAdapter> logger)
    {
        _mediaTypeService = mediaTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _mediaResolver = mediaResolver;
        _fileProcessingHandlers = fileProcessingHandlers;
        _logger = logger;
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
    public override async Task<string> FormatForLlmAsync(AISerializedEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var baseline = FormatForLlm(entity);

        // Determine the file's real MIME type — sourced from the media node's actual umbracoFile
        // property, not the (editable, unreliable) display name — so the handler check below
        // costs no I/O beyond a single media-service lookup.
        var mediaType = await _mediaResolver.GetMediaTypeAsync(entity.Unique, cancellationToken);
        if (mediaType is null || mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            // No recognized type, or an audio type — audio transcription is a paid, per-turn
            // side effect this always-on context path must never trigger. Only resolve the file
            // at all once we know a text-extraction handler actually wants it.
            return baseline;
        }

        IAIFileProcessingHandler? handler = null;
        foreach (var candidate in _fileProcessingHandlers)
        {
            if (await candidate.CanHandleAsync(mediaType, cancellationToken))
            {
                handler = candidate;
                break;
            }
        }

        if (handler is null)
        {
            return baseline;
        }

        try
        {
            var media = await _mediaResolver.ResolveAsync(entity.Unique, cancellationToken: cancellationToken);
            if (media is null)
            {
                return baseline;
            }

            var result = await handler.ProcessAsync(media.Data, media.MediaType, entity.Name, cancellationToken);
            if (string.IsNullOrWhiteSpace(result.Content))
            {
                return baseline;
            }

            return $"{baseline}\n\n### File Content\n\n{result.Content}";
        }
        catch (Exception ex)
        {
            // This context path is always-on and repeats on every turn while this media item
            // stays the active Copilot context — unlike deliberate chat attachments, a corrupted
            // or malformed file here must not fail the whole request more than once per file.
            _logger.LogWarning(ex, "Failed to extract file content for media entity {EntityUnique}; falling back to metadata-only context", entity.Unique);
            return baseline;
        }
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
