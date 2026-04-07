using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;

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
    /// <returns>The deserialized result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response text cannot be deserialized to <typeparamref name="T"/>.
    /// This typically indicates the chat was not configured with an output schema.
    /// </exception>
    public static T GetResult<T>(this ChatResponse response)
        => AIStructuredOutputParser.GetResult<T>(response.Text, "chat");

    /// <summary>
    /// Attempts to deserialize the response text as a typed structured output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="response">The chat response.</param>
    /// <param name="result">When this method returns, contains the deserialized result if successful.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult<T>(this ChatResponse response, [NotNullWhen(true)] out T? result)
        => AIStructuredOutputParser.TryGetResult(response.Text, out result);

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
        => AIStructuredOutputParser.GetJsonResult(response.Text, "chat");

    /// <summary>
    /// Attempts to parse the response text as a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <param name="result">When this method returns, contains the parsed JSON if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryGetResult(this ChatResponse response, out JsonElement result)
        => AIStructuredOutputParser.TryGetJsonResult(response.Text, out result);
}
