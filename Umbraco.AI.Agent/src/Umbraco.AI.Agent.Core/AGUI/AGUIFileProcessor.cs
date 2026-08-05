using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.AGUI.Models;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIFileProcessor"/>.
/// Stores base64 data in a thread-scoped file store and resolves stored URLs back to bytes.
/// </summary>
/// <remarks>
/// Operates on the AG-UI typed multimodal content variants (<c>image</c>, <c>audio</c>,
/// <c>video</c>, <c>document</c>) defined in <see cref="AGUIInputContent"/>.
/// </remarks>
internal sealed class AGUIFileProcessor : IAGUIFileProcessor
{
    internal const string FileIdMetadataKey = "fileId";
    internal const string FilenameMetadataKey = "filename";
    internal const string ResolvedDataMetadataKey = "__resolvedData";

    private readonly IAIFileStore _fileStore;
    private readonly IAIFileUrlProvider? _fileUrlProvider;
    private readonly IOptionsMonitor<ContentSettings> _contentSettings;
    private readonly ILogger<AGUIFileProcessor> _logger;

    public AGUIFileProcessor(
        IAIFileStore fileStore,
        IOptionsMonitor<ContentSettings> contentSettings,
        ILogger<AGUIFileProcessor> logger,
        IAIFileUrlProvider? fileUrlProvider = null)
    {
        _fileStore = fileStore;
        _contentSettings = contentSettings;
        _fileUrlProvider = fileUrlProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AGUIFileProcessorResult> ProcessInboundAsync(
        IEnumerable<AGUIMessage>? messages,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        if (messages is null)
        {
            return new AGUIFileProcessorResult
            {
                RewrittenMessages = [],
                ResolvedMessages = []
            };
        }

        var messagesList = messages.ToList();
        var rewritten = new List<AGUIMessage>(messagesList.Count);
        var resolved = new List<AGUIMessage>(messagesList.Count);
        var hasMediaContent = false;

        foreach (var message in messagesList)
        {
            if (message.ContentParts is null || !message.ContentParts.OfType<AGUIMediaInputContent>().Any())
            {
                rewritten.Add(message);
                resolved.Add(message);
                continue;
            }

            hasMediaContent = true;
            var rewrittenParts = new List<AGUIInputContent>(message.ContentParts.Count);
            var resolvedParts = new List<AGUIInputContent>(message.ContentParts.Count);

            foreach (var part in message.ContentParts)
            {
                if (part is AGUIMediaInputContent media)
                {
                    var (rewrittenPart, resolvedPart) = await ProcessMediaPartAsync(media, threadId, cancellationToken);
                    rewrittenParts.Add(rewrittenPart);
                    resolvedParts.Add(resolvedPart);
                }
                else
                {
                    rewrittenParts.Add(part);
                    resolvedParts.Add(part);
                }
            }

            rewritten.Add(CloneMessageWithParts(message, rewrittenParts));
            resolved.Add(CloneMessageWithParts(message, resolvedParts));
        }

        // If no media content was found, return same references so caller can detect no-op
        if (!hasMediaContent)
        {
            return new AGUIFileProcessorResult
            {
                RewrittenMessages = messagesList,
                ResolvedMessages = messagesList
            };
        }

        return new AGUIFileProcessorResult
        {
            RewrittenMessages = rewritten,
            ResolvedMessages = resolved
        };
    }

    private async Task<(AGUIInputContent Rewritten, AGUIInputContent Resolved)> ProcessMediaPartAsync(
        AGUIMediaInputContent part,
        string threadId,
        CancellationToken cancellationToken) => part.Source switch
        {
            AGUIInputContentDataSource dataSource => await StoreAndRewriteAsync(part, dataSource, part.Metadata, threadId, cancellationToken),
            AGUIInputContentUrlSource urlSource when TryGetFileId(part.Metadata, out var fileId) =>
                await ResolveStoredUrlAsync(part, urlSource, part.Metadata, fileId, threadId, cancellationToken),
            // External URL we can't resolve to bytes — pass through unchanged.
            _ => (part, part),
        };

    private async Task<(AGUIInputContent Rewritten, AGUIInputContent Resolved)> StoreAndRewriteAsync(
        AGUIInputContent original,
        AGUIInputContentDataSource dataSource,
        IReadOnlyDictionary<string, object?>? metadata,
        string threadId,
        CancellationToken cancellationToken)
    {
        var filename = AGUIMetadata.GetString(metadata, FilenameMetadataKey);

        // Validate file extension against CMS content settings
        var extension = Path.GetExtension(filename)?.TrimStart('.');
        if (!string.IsNullOrEmpty(extension) && !_contentSettings.CurrentValue.IsFileAllowedForUpload(extension))
        {
            _logger.LogWarning("File \"{Filename}\" has disallowed extension \"{Extension}\", skipping upload", filename, extension);
            return (original, original);
        }

        var bytes = Convert.FromBase64String(dataSource.Value);
        var fileId = await _fileStore.StoreAsync(threadId, bytes, dataSource.MimeType, filename, cancellationToken);

        _logger.LogDebug("Stored uploaded file as {FileId} ({MimeType}, {Size} bytes)", fileId, dataSource.MimeType, bytes.Length);

        var serverUrl = _fileUrlProvider?.GetFileUrl(threadId, fileId);
        var rewrittenMetadata = WithMetadata(metadata, FileIdMetadataKey, fileId);
        var resolvedMetadata = WithMetadata(metadata, FileIdMetadataKey, fileId, ResolvedDataMetadataKey, bytes);

        var rewrittenSource = serverUrl is not null
            ? (AGUIInputContentSource)new AGUIInputContentUrlSource { Value = serverUrl, MimeType = dataSource.MimeType }
            : new AGUIInputContentDataSource { Value = dataSource.Value, MimeType = dataSource.MimeType };

        return (
            AGUIInputContentFactory.FromSource(rewrittenSource, dataSource.MimeType, rewrittenMetadata),
            AGUIInputContentFactory.FromSource(rewrittenSource, dataSource.MimeType, resolvedMetadata));
    }

    private async Task<(AGUIInputContent Rewritten, AGUIInputContent Resolved)> ResolveStoredUrlAsync(
        AGUIInputContent original,
        AGUIInputContentUrlSource urlSource,
        IReadOnlyDictionary<string, object?>? metadata,
        string fileId,
        string threadId,
        CancellationToken cancellationToken)
    {
        var stored = await _fileStore.ResolveAsync(threadId, fileId, cancellationToken);
        if (stored is null)
        {
            _logger.LogWarning("Could not resolve file {FileId} for thread {ThreadId}", fileId, threadId);
            return (original, original);
        }

        // Follow-up turns may send only the file id, so fall back to the name we stored on upload.
        var resolvedMetadata = AGUIMetadata.GetString(metadata, FilenameMetadataKey) is null && stored.Filename is not null
            ? WithMetadata(metadata, ResolvedDataMetadataKey, stored.Data, FilenameMetadataKey, stored.Filename)
            : WithMetadata(metadata, ResolvedDataMetadataKey, stored.Data);

        return (
            original,
            AGUIInputContentFactory.FromSource(urlSource, urlSource.MimeType, resolvedMetadata));
    }

    private static bool TryGetFileId(IReadOnlyDictionary<string, object?>? metadata, out string fileId)
    {
        if (metadata is not null && metadata.TryGetValue(FileIdMetadataKey, out var raw) && raw is string s && !string.IsNullOrEmpty(s))
        {
            fileId = s;
            return true;
        }

        fileId = string.Empty;
        return false;
    }

    /// <summary>
    /// Returns the resolved bytes a previous call attached to an inbound part's metadata,
    /// or <c>null</c> if no resolution happened (e.g., text parts or external URLs).
    /// </summary>
    public static byte[]? GetResolvedBytes(AGUIInputContent part)
    {
        if (part is AGUIMediaInputContent media
            && media.Metadata is { } metadata
            && metadata.TryGetValue(ResolvedDataMetadataKey, out var raw)
            && raw is byte[] bytes)
        {
            return bytes;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, object?> WithMetadata(
        IReadOnlyDictionary<string, object?>? existing,
        string key,
        object? value)
        => new Dictionary<string, object?>(existing ?? new Dictionary<string, object?>(0), StringComparer.Ordinal)
        {
            [key] = value
        };

    private static IReadOnlyDictionary<string, object?> WithMetadata(
        IReadOnlyDictionary<string, object?>? existing,
        string key1,
        object? value1,
        string key2,
        object? value2)
        => new Dictionary<string, object?>(existing ?? new Dictionary<string, object?>(0), StringComparer.Ordinal)
        {
            [key1] = value1,
            [key2] = value2,
        };

    private static AGUIMessage CloneMessageWithParts(AGUIMessage original, IList<AGUIInputContent> parts)
        => new()
        {
            Id = original.Id,
            Role = original.Role,
            Content = original.Content,
            ContentParts = parts,
            Name = original.Name,
            ToolCalls = original.ToolCalls,
            ToolCallId = original.ToolCallId
        };
}
