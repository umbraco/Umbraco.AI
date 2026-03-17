using Microsoft.Extensions.AI;
using Umbraco.AI.Core.InlineChat;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Defines an AI chat service that provides access to chat completion capabilities.
/// This service acts as a thin layer over Microsoft.Extensions.AI, adding Umbraco-specific
/// features like profiles, connections, and configurable middleware.
/// </summary>
public interface IAIChatService
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
    /// The returned client has all registered middleware applied, is configured
    /// according to the specified profile, and automatically includes the profile ID
    /// for context resolution in all requests.
    /// </summary>
    /// <param name="profileId">Optional profile id. If not specified, uses the default chat profile.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IChatClient instance with middleware applied.</returns>
    Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chat response using an inline chat builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline chat via the builder.</param>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The chat completion response from the AI model.</returns>
    Task<ChatResponse> GetInlineChatResponseAsync(
        Action<AIInlineChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming chat response using an inline chat builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline chat via the builder.</param>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async stream of streaming chat updates.</returns>
    IAsyncEnumerable<ChatResponseUpdate> StreamInlineChatResponseAsync(
        Action<AIInlineChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable inline chat client with scope management per-call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned client manages runtime context scopes automatically — each call to
    /// <c>GetResponseAsync</c>/<c>GetStreamingResponseAsync</c> creates a fresh scope,
    /// sets inline-chat metadata, delegates, and disposes.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Calling methods on the returned client does not publish
    /// <see cref="InlineChat.AIChatExecutingNotification"/> or <see cref="InlineChat.AIChatExecutedNotification"/>.
    /// Use <see cref="GetInlineChatResponseAsync"/> or <see cref="StreamInlineChatResponseAsync"/>
    /// for notification support.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the inline chat via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IChatClient with inline-chat scope management.</returns>
    Task<IChatClient> CreateInlineChatClientAsync(
        Action<AIInlineChatBuilder> configure,
        CancellationToken cancellationToken = default);
}
