using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.RuntimeContext.Middleware;

/// <summary>
/// A chat client that injects multimodal content from the runtime context.
/// </summary>
/// <remarks>
/// This client monitors the runtime context for new multimodal content (images, etc.)
/// added by tools. When content is found, it injects it into the message list
/// before passing to the inner client.
/// </remarks>
internal sealed class AiRuntimeContextInjectingChatClient : DelegatingChatClient
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiRuntimeContextInjectingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client.</param>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    public AiRuntimeContextInjectingChatClient(
        IChatClient innerClient,
        IAiRuntimeContextAccessor runtimeContextAccessor)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var context = _runtimeContextAccessor.Context;

        // No context or nothing new to inject - pass through
        if (context?.IsDirty != true)
        {
            return await base.GetResponseAsync(chatMessages, options, cancellationToken);
        }

        // Inject multimodal content added by tools
        var modified = InjectMultimodalContent(chatMessages.ToList(), context);
        context.Clean();

        return await base.GetResponseAsync(modified, options, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = _runtimeContextAccessor.Context;

        // No context or nothing new to inject - pass through
        if (context?.IsDirty != true)
        {
            await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // Inject multimodal content added by tools
        var modified = InjectMultimodalContent(chatMessages.ToList(), context);
        context.Clean();

        await foreach (var update in base.GetStreamingResponseAsync(modified, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Injects multimodal content into the message list.
    /// </summary>
    private static List<ChatMessage> InjectMultimodalContent(List<ChatMessage> messages, AiRuntimeContext context)
    {
        if (context.MultimodalContents.Count == 0)
        {
            return messages;
        }

        // Create a new user message with the multimodal content
        // Add it after the last user message or at the end
        var lastUserMessageIndex = -1;
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].Role == ChatRole.User)
            {
                lastUserMessageIndex = i;
                break;
            }
        }

        // Create a new list to avoid modifying the original
        var modified = new List<ChatMessage>(messages);

        // Create a user message with the multimodal content
        var multimodalMessage = new ChatMessage(ChatRole.User, context.MultimodalContents.ToList());

        // Insert after the last user message, or add at the end
        if (lastUserMessageIndex >= 0)
        {
            modified.Insert(lastUserMessageIndex + 1, multimodalMessage);
        }
        else
        {
            modified.Add(multimodalMessage);
        }

        // Clear the content from the context (it's been injected)
        context.MultimodalContents.Clear();

        return modified;
    }
}
