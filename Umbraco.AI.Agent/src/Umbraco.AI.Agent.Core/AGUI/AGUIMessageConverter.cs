using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIMessageConverter"/>.
/// Responsible only for converting AG-UI messages to M.E.AI chat messages.
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
