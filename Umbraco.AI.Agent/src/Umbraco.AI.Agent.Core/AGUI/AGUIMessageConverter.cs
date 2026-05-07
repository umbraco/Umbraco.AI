using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Models;

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

        // Check for DataContent (binary data from LLM responses)
        var dataContents = chatMessage.Contents?.OfType<DataContent>().ToList();
        if (dataContents?.Count > 0)
        {
            var contentParts = new List<AGUIInputContent>();

            // Add text content if present
            var textContents = chatMessage.Contents?.OfType<TextContent>().ToList();
            if (textContents?.Count > 0)
            {
                foreach (var textContent in textContents)
                {
                    contentParts.Add(new AGUITextInputContent { Text = textContent.Text ?? string.Empty });
                }
            }

            // Add typed media content (AG-UI spec: image / audio / video / document by mime type)
            foreach (var dataContent in dataContents)
            {
                var mimeType = dataContent.MediaType ?? "application/octet-stream";
                var source = !dataContent.Data.IsEmpty
                    ? new AGUIInputContentDataSource
                    {
                        Value = Convert.ToBase64String(dataContent.Data.Span),
                        MimeType = mimeType,
                    }
                    : null;
                if (source is not null)
                {
                    contentParts.Add(AGUIInputContentFactory.FromSource(source, mimeType));
                }
            }

            message.ContentParts = contentParts;
        }

        // Check for function calls
        var functionCalls = chatMessage.Contents?.OfType<FunctionCallContent>().ToList();
        if (functionCalls?.Any() == true)
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

        // Check for function results
        var functionResult = chatMessage.Contents?.OfType<FunctionResultContent>().FirstOrDefault();
        if (functionResult != null)
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

            // Image / Audio / Video / Document — all share the same Source + Metadata shape.
            var (source, mimeType) = ReadMediaSource(part);
            if (source is null || mimeType is null)
            {
                continue;
            }

            // Prefer resolved bytes attached by AGUIFileProcessor.
            var resolved = AGUIFileProcessor.GetResolvedBytes(part);
            if (resolved is { Length: > 0 })
            {
                contents.Add(new DataContent(resolved, mimeType));
                continue;
            }

            switch (source)
            {
                case AGUIInputContentDataSource dataSource:
                    var bytes = Convert.FromBase64String(dataSource.Value);
                    contents.Add(new DataContent(bytes, dataSource.MimeType));
                    break;

                case AGUIInputContentUrlSource urlSource:
                    // External URL with no resolved bytes — pass through as a URL reference.
                    contents.Add(new DataContent(new Uri(urlSource.Value, UriKind.RelativeOrAbsolute), urlSource.MimeType ?? mimeType));
                    break;
            }
        }

        return new ChatMessage(role, contents);
    }

    private static (AGUIInputContentSource? Source, string? MimeType) ReadMediaSource(AGUIInputContent part) => part switch
    {
        AGUIImageInputContent img => (img.Source, MimeOf(img.Source)),
        AGUIAudioInputContent aud => (aud.Source, MimeOf(aud.Source)),
        AGUIVideoInputContent vid => (vid.Source, MimeOf(vid.Source)),
        AGUIDocumentInputContent doc => (doc.Source, MimeOf(doc.Source)),
        _ => (null, null),
    };

    private static string? MimeOf(AGUIInputContentSource source) => source switch
    {
        AGUIInputContentDataSource d => d.MimeType,
        AGUIInputContentUrlSource u => u.MimeType,
        _ => null,
    };

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
