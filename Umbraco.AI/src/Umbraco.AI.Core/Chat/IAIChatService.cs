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
    [Obsolete("Use GetChatResponseAsync for full observability (notifications, telemetry, duration tracking). Will be removed in v3. This method delegates to the inline API with alias 'chat'.")]
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
    [Obsolete("Use GetChatResponseAsync with .WithProfile(profileId) for full observability (notifications, telemetry, duration tracking). Will be removed in v3. This method delegates to the inline API with alias 'chat'.")]
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
    [Obsolete("Use StreamChatResponseAsync for full observability (notifications, telemetry, duration tracking). Will be removed in v3. This method delegates to the inline API with alias 'chat'.")]
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
    [Obsolete("Use StreamChatResponseAsync with .WithProfile(profileId) for full observability (notifications, telemetry, duration tracking). Will be removed in v3. This method delegates to the inline API with alias 'chat'.")]
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
    [Obsolete("Use CreateChatClientAsync for per-call scope management and feature metadata. Will be removed in v3. This method delegates to the inline API with alias 'chat'.")]
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
    Task<ChatResponse> GetChatResponseAsync(
        Action<AIChatBuilder> configure,
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
    IAsyncEnumerable<ChatResponseUpdate> StreamChatResponseAsync(
        Action<AIChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a structured chat response using an inline chat builder, requesting the
    /// response match type <typeparamref name="T"/>. Delegates to M.E.AI's structured
    /// output extensions which handle schema generation, response format negotiation,
    /// and deserialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="ChatResponse{T}.TryGetResult"/> for safe access when the provider
    /// may not honor structured output, or <see cref="ChatResponse{T}.Result"/> when
    /// structured output is expected to succeed. The raw response text is always available
    /// via <see cref="ChatResponse.Text"/>.
    /// </para>
    /// <para>
    /// Callers should NOT set <see cref="ChatOptions.ResponseFormat"/> via
    /// <see cref="InlineChat.AIChatBuilder.WithChatOptions"/> — the structured output
    /// extension sets it automatically based on <typeparamref name="T"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    /// <param name="configure">Action to configure the inline chat via the builder.</param>
    /// <param name="messages">The chat messages to send.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A typed chat response that can be deserialized to <typeparamref name="T"/>.</returns>
    Task<ChatResponse<T>> GetStructuredResponseAsync<T>(
        Action<AIChatBuilder> configure,
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
    /// Use <see cref="GetChatResponseAsync"/> or <see cref="StreamChatResponseAsync"/>
    /// for notification support.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the inline chat via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured IChatClient with inline-chat scope management.</returns>
    Task<IChatClient> CreateChatClientAsync(
        Action<AIChatBuilder> configure,
        CancellationToken cancellationToken = default);
}
