using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for reading structured output from <see cref="AgentResponse"/>.
/// </summary>
public static class AgentResponseExtensions
{
    /// <summary>
    /// Deserializes the response text as a typed structured output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="response">The agent response.</param>
    /// <returns>The deserialized result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response text cannot be deserialized to <typeparamref name="T"/>.
    /// This typically indicates the agent was not configured with an output schema.
    /// </exception>
    public static T GetResult<T>(this AgentResponse response)
    {
        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                $"Failed to deserialize agent response as {typeof(T).Name}: response text is empty. " +
                "Ensure the agent has an OutputSchema configured or use WithOutputSchema() to constrain the output.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize agent response as {typeof(T).Name}: deserialization returned null. " +
                    "Ensure the agent has an OutputSchema configured or use WithOutputSchema() to constrain the output.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize agent response as {typeof(T).Name}: {ex.Message}. " +
                "Ensure the agent has an OutputSchema configured or use WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize the response text as a typed structured output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="response">The agent response.</param>
    /// <param name="result">When this method returns, contains the deserialized result if successful.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult<T>(this AgentResponse response, [NotNullWhen(true)] out T? result)
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
    /// <param name="response">The agent response.</param>
    /// <returns>The parsed JSON element.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response text is not valid JSON.
    /// This typically indicates the agent was not configured with an output schema.
    /// </exception>
    public static JsonElement GetResult(this AgentResponse response)
    {
        var text = response.Text;
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException(
                "Failed to parse agent response as JSON: response text is empty. " +
                "Ensure the agent has an OutputSchema configured or use WithOutputSchema() to constrain the output.");
        }

        try
        {
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse agent response as JSON: {ex.Message}. " +
                "Ensure the agent has an OutputSchema configured or use WithOutputSchema() " +
                "to constrain the AI response to valid JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to parse the response text as a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="response">The agent response.</param>
    /// <param name="result">When this method returns, contains the parsed JSON if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult(this AgentResponse response, out JsonElement result)
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
