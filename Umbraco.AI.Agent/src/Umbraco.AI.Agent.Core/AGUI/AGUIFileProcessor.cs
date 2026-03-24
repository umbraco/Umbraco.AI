using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIFileProcessor"/>.
/// Stores base64 data in a thread-scoped file store and resolves file ID references.
/// </summary>
internal sealed class AGUIFileProcessor : IAGUIFileProcessor
{
    private readonly IAIFileStore _fileStore;
    private readonly IAIFileUrlProvider? _fileUrlProvider;
    private readonly ILogger<AGUIFileProcessor> _logger;

    public AGUIFileProcessor(IAIFileStore fileStore, ILogger<AGUIFileProcessor> logger, IAIFileUrlProvider? fileUrlProvider = null)
    {
        _fileStore = fileStore;
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
        var hasBinaryContent = false;

        foreach (var message in messagesList)
        {
            if (message.ContentParts is null || !message.ContentParts.OfType<AGUIBinaryInputContent>().Any())
            {
                rewritten.Add(message);
                resolved.Add(message);
                continue;
            }

            hasBinaryContent = true;
            var rewrittenParts = new List<AGUIInputContent>(message.ContentParts.Count);
            var resolvedParts = new List<AGUIInputContent>(message.ContentParts.Count);

            foreach (var part in message.ContentParts)
            {
                if (part is AGUIBinaryInputContent binary)
                {
                    var (rewrittenPart, resolvedPart) = await ProcessBinaryPartAsync(binary, threadId, cancellationToken);
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

        // If no binary content was found, return same references so caller can detect no-op
        if (!hasBinaryContent)
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

    private async Task<(AGUIBinaryInputContent Rewritten, AGUIBinaryInputContent Resolved)> ProcessBinaryPartAsync(
        AGUIBinaryInputContent binary,
        string threadId,
        CancellationToken cancellationToken)
    {
        // Case 1: Has base64 data — store it and rewrite to ID reference
        if (!string.IsNullOrEmpty(binary.Data))
        {
            var bytes = Convert.FromBase64String(binary.Data);
            var fileId = await _fileStore.StoreAsync(threadId, bytes, binary.MimeType, binary.Filename, cancellationToken);

            _logger.LogDebug("Stored uploaded file as {FileId} ({MimeType}, {Size} bytes)", fileId, binary.MimeType, bytes.Length);

            var rewrittenPart = new AGUIBinaryInputContent
            {
                MimeType = binary.MimeType,
                Id = fileId,
                Filename = binary.Filename,
                Url = _fileUrlProvider?.GetFileUrl(threadId, fileId)
            };

            var resolvedPart = new AGUIBinaryInputContent
            {
                MimeType = binary.MimeType,
                Id = fileId,
                Filename = binary.Filename,
                ResolvedData = bytes
            };

            return (rewrittenPart, resolvedPart);
        }

        // Case 2: Has ID reference — resolve from store
        if (!string.IsNullOrEmpty(binary.Id))
        {
            var stored = await _fileStore.ResolveAsync(threadId, binary.Id, cancellationToken);
            if (stored != null)
            {
                // Ensure the rewritten part has a URL for frontend rendering
                var rewrittenPart = binary.Url is not null
                    ? binary
                    : new AGUIBinaryInputContent
                    {
                        MimeType = binary.MimeType,
                        Id = binary.Id,
                        Filename = binary.Filename,
                        Url = _fileUrlProvider?.GetFileUrl(threadId, binary.Id)
                    };

                var resolvedPart = new AGUIBinaryInputContent
                {
                    MimeType = binary.MimeType,
                    Id = binary.Id,
                    Filename = binary.Filename,
                    ResolvedData = stored.Data
                };

                return (rewrittenPart, resolvedPart);
            }

            _logger.LogWarning("Could not resolve file {FileId} for thread {ThreadId}", binary.Id, threadId);
            return (binary, binary);
        }

        // Case 3: Has URL — pass through (URL-based resolution not yet implemented)
        return (binary, binary);
    }

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
