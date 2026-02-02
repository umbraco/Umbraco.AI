using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AIUmbracoMediaResolver> _logger;

    public AIUmbracoMediaResolver(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        ILogger<AIUmbracoMediaResolver> logger)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AIMediaContent?> ResolveAsync(object? value, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            // Try to extract a file path or media key from the value
            var (filePath, mediaKey) = ExtractPathOrKey(value);

            // If we have a media key, resolve via media service
            if (mediaKey.HasValue)
            {
                return ResolveFromMediaKey(mediaKey.Value);
            }

            // If we have a file path, read directly from storage
            if (!string.IsNullOrEmpty(filePath))
            {
                return ResolveFromPath(filePath);
            }

            _logger.LogWarning("Could not extract image path or media key from value: {ValueType}", value.GetType().Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve image from value: {ValueType}", value.GetType().Name);
            return null;
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

    private AIMediaContent? ResolveFromMediaKey(Guid mediaKey)
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

        return ResolveFromPath(filePath);
    }

    private AIMediaContent? ResolveFromPath(string filePath)
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
}
