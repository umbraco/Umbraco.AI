using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Media;

/// <summary>
/// Resolves images from media references using Umbraco's media service and file storage.
/// </summary>
internal sealed class AIUmbracoMediaResolver : IAIUmbracoMediaResolver
{
    private static readonly Dictionary<string, string> ExtensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp"
    };

    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IOptionsMonitor<AIMediaOptions> _optionsMonitor;
    private readonly ILogger<AIUmbracoMediaResolver> _logger;

    public AIUmbracoMediaResolver(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        IOptionsMonitor<AIMediaOptions> optionsMonitor,
        ILogger<AIUmbracoMediaResolver> logger)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AIMediaContent?> ResolveAsync(object? value, string? cropAlias = null, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            return Task.FromResult<AIMediaContent?>(null);
        }

        try
        {
            // Step 1: extract a raw file path or media key from the value
            var (filePath, mediaKey) = ExtractPathOrKey(value);

            AIMediaContent? content = mediaKey.HasValue
                ? LoadFromMediaKey(mediaKey.Value)
                : !string.IsNullOrEmpty(filePath) ? LoadFromPath(filePath) : null;

            if (content is null)
            {
                if (filePath is null && !mediaKey.HasValue)
                {
                    _logger.LogWarning("Could not extract image path or media key from value: {ValueType}", value.GetType().Name);
                }
                return Task.FromResult<AIMediaContent?>(null);
            }

            // Step 2: if a crop was requested, try to pull the image cropper metadata
            // from the source value (either directly from the cropper JSON, or by
            // reading the umbracoFile property of the referenced media).
            if (!string.IsNullOrWhiteSpace(cropAlias))
            {
                var cropper = TryExtractCropperPayload(value, mediaKey);
                if (cropper?.Crops is { Count: > 0 })
                {
                    content = AIImageCropper.ApplyCrop(content, cropper.Crops, cropAlias, _logger);
                }
                else
                {
                    _logger.LogWarning(
                        "Crop '{CropAlias}' requested but no image cropper data is available on the source value; using original image",
                        cropAlias);
                }
            }

            // Step 3: enforce AI provider size/dimension limits
            var downscaled = AIImageDownscaler.DownscaleIfNeeded(
                content,
                _optionsMonitor.CurrentValue,
                _logger,
                filePath ?? mediaKey?.ToString() ?? string.Empty);

            return Task.FromResult<AIMediaContent?>(downscaled);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve image from value: {ValueType}", value.GetType().Name);
            return Task.FromResult<AIMediaContent?>(null);
        }
    }

    private (string? FilePath, Guid? MediaKey) ExtractPathOrKey(object value)
    {
        // Direct Guid
        if (value is Guid guid)
        {
            return (null, guid);
        }

        // String value - could be GUID, path, or JSON
        if (value is string str)
        {
            // Try parsing as GUID
            if (Guid.TryParse(str, out var parsedGuid))
            {
                return (null, parsedGuid);
            }

            // Try parsing as JSON
            if (str.StartsWith('{') || str.StartsWith('['))
            {
                return ExtractFromJson(str);
            }

            // Treat as file path
            return (str, null);
        }

        // JsonElement
        if (value is JsonElement jsonElement)
        {
            return ExtractFromJsonElement(jsonElement);
        }

        return (null, null);
    }

    private (string? FilePath, Guid? MediaKey) ExtractFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return ExtractFromJsonElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private (string? FilePath, Guid? MediaKey) ExtractFromJsonElement(JsonElement element)
    {
        // Array format: [{"mediaKey": "guid"}] - media picker v3
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var result = ExtractFromJsonElement(item);
                if (result.FilePath is not null || result.MediaKey.HasValue)
                {
                    return result;
                }
            }
            return (null, null);
        }

        // Object format
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Media picker v3 format: {"mediaKey": "guid"}
            if (element.TryGetProperty("mediaKey", out var mediaKeyProp))
            {
                var keyStr = mediaKeyProp.GetString();
                if (!string.IsNullOrEmpty(keyStr) && Guid.TryParse(keyStr, out var mediaGuid))
                {
                    return (null, mediaGuid);
                }
            }

            // Image cropper format: {"src": "/media/..."}
            if (element.TryGetProperty("src", out var srcProp))
            {
                var src = srcProp.GetString();
                if (!string.IsNullOrEmpty(src))
                {
                    return (src, null);
                }
            }

            // Legacy media picker format: {"key": "guid"} or {"udi": "umb://media/guid"}
            if (element.TryGetProperty("key", out var keyProp))
            {
                var keyStr = keyProp.GetString();
                if (!string.IsNullOrEmpty(keyStr) && Guid.TryParse(keyStr, out var mediaGuid))
                {
                    return (null, mediaGuid);
                }
            }
        }

        return (null, null);
    }

    private AIMediaContent? LoadFromMediaKey(Guid mediaKey)
    {
        var media = _mediaService.GetById(mediaKey);
        if (media is null)
        {
            _logger.LogWarning("Media not found for key: {MediaKey}", mediaKey);
            return null;
        }

        // Get the umbracoFile property value
        var umbracoFile = media.GetValue<string>("umbracoFile");
        if (string.IsNullOrEmpty(umbracoFile))
        {
            _logger.LogWarning("Media {MediaKey} has no umbracoFile property", mediaKey);
            return null;
        }

        // umbracoFile might be JSON (image cropper) or plain path
        string? filePath;
        if (umbracoFile.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(umbracoFile);
                filePath = doc.RootElement.TryGetProperty("src", out var srcProp) ? srcProp.GetString() : null;
            }
            catch (JsonException)
            {
                filePath = umbracoFile;
            }
        }
        else
        {
            filePath = umbracoFile;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        return LoadFromPath(filePath);
    }

    private AIMediaContent? LoadFromPath(string filePath)
    {
        // Normalize the path - remove leading /media/ if present since MediaFileManager works with relative paths
        var relativePath = filePath;
        if (relativePath.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[7..]; // Remove "/media/"
        }
        else if (relativePath.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[6..]; // Remove "media/"
        }

        // Get file extension for media type
        var extension = Path.GetExtension(filePath);
        if (!ExtensionToMediaType.TryGetValue(extension, out var mediaType))
        {
            _logger.LogWarning("Unsupported image extension: {Extension}", extension);
            return null;
        }

        // Read file from media storage
        var fileSystem = _mediaFileManager.FileSystem;
        if (!fileSystem.FileExists(relativePath))
        {
            _logger.LogWarning("Media file not found: {FilePath}", relativePath);
            return null;
        }

        using var stream = fileSystem.OpenFile(relativePath);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return new AIMediaContent
        {
            Data = memoryStream.ToArray(),
            MediaType = mediaType
        };
    }

    private ImageCropperPayload? TryExtractCropperPayload(object value, Guid? mediaKey)
    {
        // Case 1: we resolved via a media key — read the umbracoFile property from
        // the media and try to parse it as an image cropper payload.
        if (mediaKey.HasValue)
        {
            var media = _mediaService.GetById(mediaKey.Value);
            var umbracoFile = media?.GetValue<string>("umbracoFile");
            return TryParseCropperJson(umbracoFile);
        }

        // Case 2: value is cropper JSON directly.
        if (value is string str && str.StartsWith('{'))
        {
            return TryParseCropperJson(str);
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Object } element)
        {
            return TryParseCropperJson(element.GetRawText());
        }

        return null;
    }

    private ImageCropperPayload? TryParseCropperJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ImageCropperPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse image cropper payload; crop selection will fall back to original image");
            return null;
        }
    }
}
