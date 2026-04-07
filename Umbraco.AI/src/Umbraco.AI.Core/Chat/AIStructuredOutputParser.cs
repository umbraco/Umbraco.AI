using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Shared parser for reading structured output from AI response text.
/// Used by extension methods on both <c>ChatResponse</c> and <c>AgentResponse</c>.
/// </summary>
internal static class AIStructuredOutputParser
{
    internal static T GetResult<T>(string? text, string responseKind)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                $"Failed to deserialize {responseKind} response as {typeof(T).Name}: response text is empty. " +
                $"Ensure the {responseKind} is configured with an output schema via WithOutputSchema().");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize {responseKind} response as {typeof(T).Name}: deserialization returned null. " +
                    $"Ensure the {responseKind} is configured with an output schema via WithOutputSchema().");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize {responseKind} response as {typeof(T).Name}: {ex.Message}. " +
                $"Ensure the {responseKind} is configured with an output schema via WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    internal static bool TryGetResult<T>(string? text, [NotNullWhen(true)] out T? result)
    {
        result = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(text);
            return result is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static JsonElement GetJsonResult(string? text, string responseKind)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                $"Failed to parse {responseKind} response as JSON: response text is empty. " +
                $"Ensure the {responseKind} is configured with an output schema via WithOutputSchema().");
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse {responseKind} response as JSON: {ex.Message}. " +
                $"Ensure the {responseKind} is configured with an output schema via WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    internal static bool TryGetJsonResult(string? text, out JsonElement result)
    {
        result = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            result = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
