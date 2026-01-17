using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// Base class for chat client decorators that bind context (e.g., profile or agent) to requests.
/// </summary>
/// <remarks>
/// Provides virtual methods to transform messages and options before delegation.
/// Subclasses override <see cref="TransformMessages"/> and/or <see cref="TransformOptions"/>
/// to customize behavior.
/// </remarks>
public abstract class AiBoundChatClientBase : IChatClient
{
    /// <summary>
    /// Gets the inner chat client that this decorator delegates to.
    /// </summary>
    protected IChatClient InnerClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiBoundChatClientBase"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    protected AiBoundChatClientBase(IChatClient innerClient)
    {
        InnerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    /// <inheritdoc />
    public virtual Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return InnerClient.GetResponseAsync(
            TransformMessages(chatMessages),
            TransformOptions(options),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in InnerClient.GetStreamingResponseAsync(
            TransformMessages(chatMessages),
            TransformOptions(options),
            cancellationToken))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? key = null)
    {
        // Return self if this exact type is requested
        if (serviceType == GetType())
        {
            return this;
        }

        // Delegate to inner client for other services
        return InnerClient.GetService(serviceType, key);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        InnerClient.Dispose();
    }

    /// <summary>
    /// Transforms the chat messages before passing to the inner client.
    /// </summary>
    /// <param name="chatMessages">The original chat messages.</param>
    /// <returns>The transformed chat messages.</returns>
    protected virtual IEnumerable<ChatMessage> TransformMessages(IEnumerable<ChatMessage> chatMessages)
        => chatMessages;

    /// <summary>
    /// Transforms the chat options before passing to the inner client.
    /// </summary>
    /// <param name="options">The original chat options.</param>
    /// <returns>The transformed chat options.</returns>
    protected virtual ChatOptions? TransformOptions(ChatOptions? options)
        => options;
}
