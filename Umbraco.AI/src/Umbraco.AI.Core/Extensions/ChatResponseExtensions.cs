using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for reading structured output from <see cref="ChatResponse"/>.
/// </summary>
public static class ChatResponseExtensions
{
    /// <summary>
    /// Deserializes the response text as a typed structured output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="response">The chat response.</param>
    /// <returns>The deserialized result, or <c>default</c> if deserialization fails.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response text cannot be deserialized to <typeparamref name="T"/>.
    /// This typically indicates the chat was not configured with an output schema.
    /// </exception>
    public static T GetResult<T>(this ChatResponse response)
    {
        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                $"Failed to deserialize chat response as {typeof(T).Name}: response text is empty. " +
                "Ensure the chat is configured with an output schema via WithOutputSchema().");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize chat response as {typeof(T).Name}: deserialization returned null. " +
                    "Ensure the chat is configured with an output schema via WithOutputSchema().");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize chat response as {typeof(T).Name}: {ex.Message}. " +
                "Ensure the chat is configured with an output schema via WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize the response text as a typed structured output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="response">The chat response.</param>
    /// <param name="result">When this method returns, contains the deserialized result if successful.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult<T>(this ChatResponse response, [NotNullWhen(true)] out T? result)
    {
        result = default;
        var text = response.Text;
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

    /// <summary>
    /// Parses the response text as a <see cref="JsonElement"/> for runtime schema scenarios.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <returns>The parsed JSON element.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response text is not valid JSON.
    /// This typically indicates the chat was not configured with an output schema.
    /// </exception>
    public static JsonElement GetResult(this ChatResponse response)
    {
        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                "Failed to parse chat response as JSON: response text is empty. " +
                "Ensure the chat is configured with an output schema via WithOutputSchema().");
        }

        try
        {
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse chat response as JSON: {ex.Message}. " +
                "Ensure the chat is configured with an output schema via WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to parse the response text as a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <param name="result">When this method returns, contains the parsed JSON if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult(this ChatResponse response, out JsonElement result)
    {
        result = default;
        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        try
        {
            result = JsonDocument.Parse(text).RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
