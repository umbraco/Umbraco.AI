using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// Defines an AI chat service that provides access to chat completion capabilities.
/// This service acts as a thin layer over Microsoft.Extensions.AI, adding Umbraco-specific
/// features like profiles, connections, and configurable middleware.
/// </summary>
public interface IAiChatService
{
    /// <summary>
    /// Gets a chat response using the default profile.
    /// Profile settings (temperature, max tokens, model) are used as defaults
    /// and can be overridden by options.
    /// </summary>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="options">Optional chat options to override profile defaults.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The chat completion response from the AI model.</returns>
    Task<ChatResponse> GetChatResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chat response using a specific named profile.
    /// Profile settings (temperature, max tokens, model) are used as defaults
    /// and can be overridden by options.
    /// </summary>
    /// <param name="profileId">The ID of the profile to use.</param>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="options">Optional chat options to override profile defaults.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The chat completion response from the AI model.</returns>
    Task<ChatResponse> GetChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming chat response using the default profile.
    /// Profile settings (temperature, max tokens, model) are used as defaults
    /// and can be overridden by options.
    /// </summary>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="options">Optional chat options to override profile defaults.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async stream of streaming chat updates.</returns>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming chat response using a specific named profile.
    /// Profile settings (temperature, max tokens, model) are used as defaults
    /// and can be overridden by options.
    /// </summary>
    /// <param name="profileId">The ID of the profile to use.</param>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="options">Optional chat options to override profile defaults.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async stream of streaming chat updates.</returns>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configured chat client for advanced scenarios.
    /// The returned client has all registered middleware applied and is configured
    /// according to the specified profile.
    /// </summary>
    /// <param name="profileId">Optional profile id. If not specified, uses the default chat profile.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IChatClient instance with middleware applied.</returns>
    Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);
}
