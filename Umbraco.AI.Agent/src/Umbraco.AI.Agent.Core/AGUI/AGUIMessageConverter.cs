using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIMessageConverter"/>.
/// Responsible only for converting AG-UI messages to M.E.AI chat messages.
/// Handles both plain text and multimodal content parts.
/// </summary>
internal sealed class AGUIMessageConverter : IAGUIMessageConverter
{
    /// <inheritdoc />
    public List<ChatMessage> ConvertToChatMessages(IEnumerable<AGUIMessage>? messages)
    {
        var chatMessages = new List<ChatMessage>();

        if (messages != null)
        {
            foreach (var msg in messages)
            {
                chatMessages.Add(ConvertToChatMessage(msg));
            }
        }

        return chatMessages;
    }

    /// <inheritdoc />
    public ChatMessage ConvertToChatMessage(AGUIMessage message)
    {
        // Assistant message with tool calls - include FunctionCallContent
        if (message.Role == AGUIMessageRole.Assistant && message.ToolCalls?.Any() == true)
        {
            return ConvertAssistantMessageWithToolCalls(message);
        }

        // Tool result message - include FunctionResultContent
        if (message.Role == AGUIMessageRole.Tool && !string.IsNullOrEmpty(message.ToolCallId))
        {
            return ConvertToolResultMessage(message);
        }

        // Multimodal message with content parts
        if (message.ContentParts is { Count: > 0 })
        {
            return ConvertMultimodalMessage(message);
        }

        // Regular message
        var role = ConvertToChatRole(message.Role);
        return new ChatMessage(role, message.Content ?? string.Empty);
    }

    /// <inheritdoc />
    public AGUIMessage ConvertFromChatMessage(ChatMessage chatMessage)
    {
        var role = ConvertFromChatRole(chatMessage.Role);
        var message = new AGUIMessage
        {
            // AG-UI requires id on every message — prefer the upstream MessageId, otherwise mint one.
            Id = chatMessage.MessageId ?? Guid.NewGuid().ToString(),
            Role = role,
            Content = chatMessage.Text
        };

        // Single pass over Contents to bucket text / data / function-call / function-result.
        List<TextContent>? textContents = null;
        List<DataContent>? dataContents = null;
        List<UriContent>? uriContents = null;
        List<FunctionCallContent>? functionCalls = null;
        FunctionResultContent? functionResult = null;

        if (chatMessage.Contents is not null)
        {
            foreach (var content in chatMessage.Contents)
            {
                switch (content)
                {
                    case TextContent t:
                        (textContents ??= []).Add(t);
                        break;
                    case DataContent d:
                        (dataContents ??= []).Add(d);
                        break;
                    case UriContent u:
                        (uriContents ??= []).Add(u);
                        break;
                    case FunctionCallContent fc:
                        (functionCalls ??= []).Add(fc);
                        break;
                    case FunctionResultContent fr when functionResult is null:
                        functionResult = fr;
                        break;
                }
            }
        }

        if (dataContents is { Count: > 0 } || uriContents is { Count: > 0 })
        {
            var contentParts = new List<AGUIInputContent>(
                (textContents?.Count ?? 0) + (dataContents?.Count ?? 0) + (uriContents?.Count ?? 0));

            if (textContents is { Count: > 0 })
            {
                foreach (var textContent in textContents)
                {
                    contentParts.Add(new AGUITextInputContent { Text = textContent.Text ?? string.Empty });
                }
            }

            foreach (var dataContent in dataContents ?? [])
            {
                if (dataContent.Data.IsEmpty)
                {
                    continue;
                }

                var mimeType = dataContent.MediaType ?? "application/octet-stream";
                var source = new AGUIInputContentDataSource
                {
                    Value = Convert.ToBase64String(dataContent.Data.Span),
                    MimeType = mimeType,
                };
                contentParts.Add(AGUIInputContentFactory.FromSource(source, mimeType, BuildFilenameMetadata(dataContent.Name)));
            }

            foreach (var uriContent in uriContents ?? [])
            {
                var mimeType = uriContent.MediaType ?? "application/octet-stream";
                var source = new AGUIInputContentUrlSource
                {
                    Value = uriContent.Uri.ToString(),
                    MimeType = mimeType,
                };
                contentParts.Add(AGUIInputContentFactory.FromSource(source, mimeType));
            }

            message.ContentParts = contentParts;
        }

        if (functionCalls is { Count: > 0 })
        {
            message.ToolCalls = functionCalls.Select(fc => new AGUIToolCall
            {
                Id = fc.CallId,
                Type = "function",
                Function = new AGUIFunctionCall
                {
                    Name = fc.Name,
                    Arguments = fc.Arguments != null
                        ? JsonSerializer.Serialize(fc.Arguments)
                        : "{}"
                }
            }).ToList();
        }

        if (functionResult is not null)
        {
            message.ToolCallId = functionResult.CallId;
            message.Content = functionResult.Result?.ToString() ?? string.Empty;
        }

        return message;
    }

    private static ChatMessage ConvertMultimodalMessage(AGUIMessage message)
    {
        var role = ConvertToChatRole(message.Role);
        var contents = new List<AIContent>();

        foreach (var part in message.ContentParts!)
        {
            if (part is AGUITextInputContent textPart)
            {
                contents.Add(new TextContent(textPart.Text));
                continue;
            }

            if (part is not AGUIMediaInputContent media)
            {
                continue;
            }

            var mimeType = media.Source.GetMimeType();
            if (mimeType is null)
            {
                continue;
            }

            // The uploaded filename travels in metadata — carry it onto the content so file
            // processing handlers receive a real name instead of falling back to the data URI.
            var filename = GetFilename(media.Metadata);

            // Prefer resolved bytes attached by AGUIFileProcessor.
            var resolved = AGUIFileProcessor.GetResolvedBytes(media);
            if (resolved is { Length: > 0 })
            {
                var dataContent = new DataContent(resolved, mimeType) { Name = filename };

                // Tag the content with the id it's already stored under in IAIFileStore, so a
                // consumer persisting this message (a persisted conversation, say) knows it can write
                // a lightweight reference instead of freezing these bytes into its own storage too.
                var fileId = AGUIMetadata.GetString(media.Metadata, AGUIFileProcessor.FileIdMetadataKey);
                if (fileId is not null)
                {
                    dataContent.AdditionalProperties = new AdditionalPropertiesDictionary
                    {
                        [AIFileContentMarker.FileIdPropertyKey] = fileId
                    };
                }

                contents.Add(dataContent);
                continue;
            }

            switch (media.Source)
            {
                case AGUIInputContentDataSource dataSource:
                    var bytes = Convert.FromBase64String(dataSource.Value);
                    contents.Add(new DataContent(bytes, dataSource.MimeType) { Name = filename });
                    break;

                case AGUIInputContentUrlSource urlSource:
                    // DataContent only accepts data URIs — a remote reference has to be UriContent.
                    contents.Add(new UriContent(new Uri(urlSource.Value, UriKind.RelativeOrAbsolute), urlSource.MimeType ?? mimeType));
                    break;
            }
        }

        return new ChatMessage(role, contents);
    }

    private static string? GetFilename(IReadOnlyDictionary<string, object?>? metadata)
        => AGUIMetadata.GetString(metadata, AGUIFileProcessor.FilenameMetadataKey);

    private static IReadOnlyDictionary<string, object?>? BuildFilenameMetadata(string? filename)
        => string.IsNullOrWhiteSpace(filename)
            ? null
            : new Dictionary<string, object?> { [AGUIFileProcessor.FilenameMetadataKey] = filename };

    private static ChatMessage ConvertAssistantMessageWithToolCalls(AGUIMessage message)
    {
        var contents = new List<AIContent>();

        // Add text content if present
        if (!string.IsNullOrEmpty(message.Content))
        {
            contents.Add(new TextContent(message.Content));
        }

        // Add function call content for each tool call
        foreach (var toolCall in message.ToolCalls!)
        {
            var args = ParseArguments(toolCall.Function.Arguments);
            contents.Add(new FunctionCallContent(toolCall.Id, toolCall.Function.Name, args));
        }

        return new ChatMessage(ChatRole.Assistant, contents);
    }

    private static ChatMessage ConvertToolResultMessage(AGUIMessage message)
    {
        var result = new FunctionResultContent(message.ToolCallId!, message.Content ?? string.Empty);
        return new ChatMessage(ChatRole.Tool, [result]);
    }

    private static IDictionary<string, object?>? ParseArguments(string? argumentsJson)
    {
        if (string.IsNullOrEmpty(argumentsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
        }
        catch
        {
            // If parsing fails, return empty dict
            return new Dictionary<string, object?>();
        }
    }

    private static ChatRole ConvertToChatRole(AGUIMessageRole role) => role switch
    {
        AGUIMessageRole.User => ChatRole.User,
        AGUIMessageRole.Assistant => ChatRole.Assistant,
        AGUIMessageRole.System => ChatRole.System,
        AGUIMessageRole.Tool => ChatRole.Tool,
        AGUIMessageRole.Developer => ChatRole.System, // Map developer to system
        _ => ChatRole.User
    };

    private static AGUIMessageRole ConvertFromChatRole(ChatRole role)
    {
        if (role == ChatRole.User) return AGUIMessageRole.User;
        if (role == ChatRole.Assistant) return AGUIMessageRole.Assistant;
        if (role == ChatRole.System) return AGUIMessageRole.System;
        if (role == ChatRole.Tool) return AGUIMessageRole.Tool;
        return AGUIMessageRole.User;
    }

}
