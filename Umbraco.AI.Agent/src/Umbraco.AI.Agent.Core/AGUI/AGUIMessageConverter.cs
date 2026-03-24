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

            // Add binary content
            foreach (var dataContent in dataContents)
            {
                contentParts.Add(new AGUIBinaryInputContent
                {
                    MimeType = dataContent.MediaType ?? "application/octet-stream",
                    Data = !dataContent.Data.IsEmpty ? Convert.ToBase64String(dataContent.Data.Span) : null
                });
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
            switch (part)
            {
                case AGUITextInputContent textPart:
                    contents.Add(new TextContent(textPart.Text));
                    break;

                case AGUIBinaryInputContent binaryPart:
                    if (binaryPart.ResolvedData is { Length: > 0 })
                    {
                        // Use resolved bytes from file processor
                        contents.Add(new DataContent(binaryPart.ResolvedData, binaryPart.MimeType));
                    }
                    else if (!string.IsNullOrEmpty(binaryPart.Data))
                    {
                        // Fallback: decode inline base64
                        var bytes = Convert.FromBase64String(binaryPart.Data);
                        contents.Add(new DataContent(bytes, binaryPart.MimeType));
                    }
                    else if (!string.IsNullOrEmpty(binaryPart.Id))
                    {
                        // Has a file store ID but no resolved data — skip.
                        // The file processor should have resolved this; if it didn't,
                        // the file may have expired or been cleaned up.
                    }
                    else if (!string.IsNullOrEmpty(binaryPart.Url))
                    {
                        // External URL-based content (not from file store)
                        contents.Add(new DataContent(new Uri(binaryPart.Url, UriKind.RelativeOrAbsolute), binaryPart.MimeType));
                    }
                    break;
            }
        }

        return new ChatMessage(role, contents);
    }

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
