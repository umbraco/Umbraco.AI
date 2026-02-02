using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Provides formatting utilities for <see cref="ChatMessage"/> objects in audit logs.
/// </summary>
internal static class AiChatMessageFormatter
{
    private const int MaxArgumentsLength = 500;
    private const int MaxResultLength = 1000;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        MaxDepth = 10
    };

    /// <summary>
    /// Formats a collection of chat messages for audit log storage.
    /// </summary>
    /// <param name="messages">The chat messages to format.</param>
    /// <returns>A formatted string representation of the messages.</returns>
    public static string FormatChatMessages(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        var isFirst = true;

        foreach (var message in messages)
        {
            if (!isFirst)
            {
                sb.AppendLine();
            }
            isFirst = false;

            FormatChatMessage(sb, message);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a single chat message for audit log storage.
    /// </summary>
    /// <param name="message">The chat message to format.</param>
    /// <returns>A formatted string representation of the message.</returns>
    public static string FormatChatMessage(ChatMessage message)
    {
        var sb = new StringBuilder();
        FormatChatMessage(sb, message);
        return sb.ToString();
    }

    private static void FormatChatMessage(StringBuilder sb, ChatMessage message)
    {
        var hasTextContent = false;

        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    // Text content goes on the role line
                    if (!hasTextContent)
                    {
                        sb.Append($"[{message.Role}] {textContent.Text}");
                        hasTextContent = true;
                    }
                    else
                    {
                        // Additional text content on same line
                        sb.Append(textContent.Text);
                    }
                    break;

                case FunctionCallContent functionCall:
                    // Function calls need a new line if we already have text
                    if (hasTextContent)
                    {
                        sb.AppendLine();
                    }
                    FormatFunctionCall(sb, functionCall);
                    break;

                case FunctionResultContent functionResult:
                    // Function results are shown as a tool role
                    FormatFunctionResult(sb, functionResult);
                    hasTextContent = true; // Mark as having content
                    break;

                case DataContent dataContent:
                    if (hasTextContent)
                    {
                        sb.AppendLine();
                    }
                    FormatDataContent(sb, dataContent);
                    break;

                default:
                    // Unknown content types
                    if (hasTextContent)
                    {
                        sb.AppendLine();
                    }
                    sb.Append($"  [unknown:{content.GetType().Name}]");
                    break;
            }
        }

        // If no content was added, just output the role
        if (!hasTextContent && sb.Length == 0)
        {
            sb.Append($"[{message.Role}]");
        }
    }

    private static void FormatFunctionCall(StringBuilder sb, FunctionCallContent functionCall)
    {
        var callId = functionCall.CallId ?? "unknown";
        var name = functionCall.Name ?? "unknown";
        var args = FormatArguments(functionCall.Arguments);

        sb.Append($"  [tool_call:{callId}] {name}({args})");
    }

    private static void FormatFunctionResult(StringBuilder sb, FunctionResultContent functionResult)
    {
        var callId = functionResult.CallId ?? "unknown";
        var result = FormatResult(functionResult.Result);

        sb.Append($"[tool:{callId}] -> {result}");
    }

    private static void FormatDataContent(StringBuilder sb, DataContent dataContent)
    {
        var mimeType = dataContent.MediaType ?? "unknown";
        var size = dataContent.Data.Length;

        sb.Append($"  [data:{mimeType}] ({size} bytes)");
    }

    private static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "{}";
        }

        try
        {
            var json = JsonSerializer.Serialize(arguments, s_jsonOptions);
            return TruncateIfNeeded(json, MaxArgumentsLength);
        }
        catch
        {
            return "[serialization error]";
        }
    }

    private static string FormatResult(object? result)
    {
        if (result is null)
        {
            return "(null)";
        }

        try
        {
            var json = result is string str ? str : JsonSerializer.Serialize(result, s_jsonOptions);
            return TruncateIfNeeded(json, MaxResultLength);
        }
        catch
        {
            return "[serialization error]";
        }
    }

    private static string TruncateIfNeeded(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}... (truncated, {value.Length} chars)";
    }
}
