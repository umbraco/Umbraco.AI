using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// A delegating chat client that processes uploaded files in chat messages,
/// converting supported file types into text content before passing to the inner client.
/// </summary>
internal sealed class AIFileProcessingChatClient : DelegatingChatClient
{
    private readonly AIFileProcessingHandlerCollection _handlers;
    private readonly IOptionsMonitor<ContentSettings> _contentSettings;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileProcessingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="handlers">The collection of file processing handlers.</param>
    /// <param name="contentSettings">The CMS content settings for file upload validation.</param>
    /// <param name="logger">The logger.</param>
    public AIFileProcessingChatClient(
        IChatClient innerClient,
        AIFileProcessingHandlerCollection handlers,
        IOptionsMonitor<ContentSettings> contentSettings,
        ILogger logger)
        : base(innerClient)
    {
        _handlers = handlers;
        _contentSettings = contentSettings;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processedMessages = await ProcessMessagesAsync(chatMessages, cancellationToken);
        return await InnerClient.GetResponseAsync(processedMessages, options, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var processedMessages = await ProcessMessagesAsync(chatMessages, cancellationToken);

        await foreach (var update in InnerClient.GetStreamingResponseAsync(processedMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private async Task<IList<ChatMessage>> ProcessMessagesAsync(
        IEnumerable<ChatMessage> chatMessages,
        CancellationToken cancellationToken)
    {
        var messages = chatMessages.ToList();
        var hasDataContent = false;

        // Quick check: any DataContent to process?
        foreach (var message in messages)
        {
            if (message.Contents.Any(c => c is DataContent))
            {
                hasDataContent = true;
                break;
            }
        }

        if (!hasDataContent)
        {
            return messages;
        }

        var result = new List<ChatMessage>(messages.Count);

        foreach (var message in messages)
        {
            if (!message.Contents.Any(c => c is DataContent))
            {
                result.Add(message);
                continue;
            }

            var processedContents = new List<AIContent>(message.Contents.Count);

            foreach (var content in message.Contents)
            {
                if (content is not DataContent dataContent || dataContent.MediaType is null)
                {
                    processedContents.Add(content);
                    continue;
                }

                // Name is the only place a real filename can come from: DataContent.Uri is always a
                // data URI, so treating it as a path just yields the tail of the base64 payload.
                var filename = !string.IsNullOrEmpty(dataContent.Name)
                    ? dataContent.Name
                    : null;

                // Validate file extension against CMS content settings
                if (filename is not null)
                {
                    var extension = Path.GetExtension(filename)?.TrimStart('.');
                    if (!string.IsNullOrEmpty(extension) && !_contentSettings.CurrentValue.IsFileAllowedForUpload(extension))
                    {
                        _logger.LogWarning("File \"{Filename}\" has disallowed extension \"{Extension}\", skipping", filename, extension);
                        continue;
                    }
                }

                var effectiveMimeType = dataContent.MediaType;
                var handler = await FindHandlerAsync(effectiveMimeType, cancellationToken);

                if (handler is null && filename is not null)
                {
                    // The browser/client-reported MIME type is frequently wrong for the exact
                    // file types this feature targets (e.g. Windows often reports .md as
                    // application/octet-stream, and .csv as application/vnd.ms-excel). Fall back
                    // to resolving a MIME type from the filename's extension and retry.
                    var extension = Path.GetExtension(filename);
                    if (AIMediaExtensionResolver.TryGetMediaType(extension, out var extensionMimeType))
                    {
                        var extensionHandler = await FindHandlerAsync(extensionMimeType, cancellationToken);
                        if (extensionHandler is not null)
                        {
                            handler = extensionHandler;
                            effectiveMimeType = extensionMimeType;
                        }
                    }
                }

                if (handler is null)
                {
                    // No handler for this type — pass through (images, PDFs, etc.)
                    processedContents.Add(content);
                    continue;
                }

                var processingResult = await handler.ProcessAsync(
                    dataContent.Data,
                    effectiveMimeType,
                    filename,
                    cancellationToken);

                var label = filename is not null
                    ? $"[File: {filename}]\n"
                    : string.Empty;

                processedContents.Add(new TextContent($"{label}{processingResult.Content}"));
            }

            var processedMessage = new ChatMessage(message.Role, processedContents)
            {
                AuthorName = message.AuthorName,
            };

            // Copy additional properties
            foreach (var kvp in message.AdditionalProperties ?? [])
            {
                processedMessage.AdditionalProperties ??= [];
                processedMessage.AdditionalProperties[kvp.Key] = kvp.Value;
            }

            result.Add(processedMessage);
        }

        return result;
    }

    private async Task<IAIFileProcessingHandler?> FindHandlerAsync(string mimeType, CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
        {
            if (await handler.CanHandleAsync(mimeType, cancellationToken))
            {
                return handler;
            }
        }

        return null;
    }
}
