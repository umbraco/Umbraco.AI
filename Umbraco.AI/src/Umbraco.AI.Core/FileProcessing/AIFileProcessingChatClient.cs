using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// A delegating chat client that processes uploaded files in chat messages,
/// converting supported file types into text content before passing to the inner client.
/// </summary>
internal sealed class AIFileProcessingChatClient : DelegatingChatClient
{
    private readonly AIFileProcessingHandlerCollection _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileProcessingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="handlers">The collection of file processing handlers.</param>
    public AIFileProcessingChatClient(
        IChatClient innerClient,
        AIFileProcessingHandlerCollection handlers)
        : base(innerClient)
    {
        _handlers = handlers;
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

                var handler = FindHandler(dataContent.MediaType);
                if (handler is null)
                {
                    // No handler for this type — pass through (images, PDFs, etc.)
                    processedContents.Add(content);
                    continue;
                }

                var filename = !string.IsNullOrEmpty(dataContent.Name)
                    ? dataContent.Name
                    : GetFilenameFromUri(dataContent.Uri);

                var processingResult = await handler.ProcessAsync(
                    dataContent.Data,
                    dataContent.MediaType,
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

    private IAIFileProcessingHandler? FindHandler(string mimeType)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(mimeType))
            {
                return handler;
            }
        }

        return null;
    }

    private static string? GetFilenameFromUri(string? uri)
        => uri is null ? null : Path.GetFileName(uri);
}
