using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Provides formatting utilities for <see cref="ChatMessage"/> objects in audit logs.
/// </summary>
internal static class AIChatMessageFormatter
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
        sb.Append($"===== [{message.Role}] =====");

        var lastWasText = false;

        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    if (lastWasText)
                    {
                        // Additional text content continues on the same line
                        sb.Append(textContent.Text);
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.Append(textContent.Text);
                    }
                    lastWasText = true;
                    break;

                case FunctionCallContent functionCall:
                    sb.AppendLine();
                    FormatFunctionCall(sb, functionCall);
                    lastWasText = false;
                    break;

                case FunctionResultContent functionResult:
                    sb.AppendLine();
                    FormatFunctionResult(sb, functionResult);
                    lastWasText = false;
                    break;

                case DataContent dataContent:
                    sb.AppendLine();
                    FormatDataContent(sb, dataContent);
                    lastWasText = false;
                    break;

                default:
                    // Unknown content types
                    sb.AppendLine();
                    sb.Append($"  [unknown:{content.GetType().Name}]");
                    lastWasText = false;
                    break;
            }
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
