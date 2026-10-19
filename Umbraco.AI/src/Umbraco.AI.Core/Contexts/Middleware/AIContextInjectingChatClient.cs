using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Contexts.Middleware;

/// <summary>
/// A delegating chat client that injects AI context into chat requests.
/// </summary>
/// <remarks>
/// This client:
/// - Resolves context using all registered resolvers via <see cref="IAIContextResolutionService"/>
/// - Injects "Always" mode resources into the system prompt
/// - Makes the resolved context available via <see cref="IAIContextAccessor"/> for OnDemand tools
/// </remarks>
internal sealed class AIContextInjectingChatClient : DelegatingChatClient
{
    private readonly IAIContextResolutionService _contextResolutionService;
    private readonly IAIContextProcessor _contextProcessor;
    private readonly IAIContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextInjectingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="contextResolutionService">The context resolution service.</param>
    /// <param name="contextProcessor">The context formatter.</param>
    /// <param name="contextAccessor">The context accessor for tool access.</param>
    public AIContextInjectingChatClient(
        IChatClient innerClient,
        IAIContextResolutionService contextResolutionService,
        IAIContextProcessor contextProcessor,
        IAIContextAccessor contextAccessor)
        : base(innerClient)
    {
        _contextResolutionService = contextResolutionService;
        _contextProcessor = contextProcessor;
        _contextAccessor = contextAccessor;
    }

    #region IChatClient

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var (modifiedMessages, resolvedContext) = await PrepareContextAsync(messagesList, cancellationToken);

        // Set here, not in PrepareContextAsync: the context is held per async flow, so one set inside an awaited
        // method would not be current once it returns.
        using var contextScope = _contextAccessor.SetContext(resolvedContext);
        return await InnerClient.GetResponseAsync(modifiedMessages, options, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var (modifiedMessages, resolvedContext) = await PrepareContextAsync(messagesList, cancellationToken);

        // Re-entered around each step: the context is held per async flow, and this iterator's steps run in its
        // caller's flow, so tools run on later steps (after a tool call) still see this call's context.
        using var contextScope = _contextAccessor.SetContext(resolvedContext);
        await foreach (var update in InnerClient.GetStreamingResponseAsync(modifiedMessages, options, cancellationToken)
                           .EnterEachStep(() => AIContextAccessor.Enter(contextScope)))
        {
            yield return update;
        }
    }

    #endregion

    private async Task<(IList<ChatMessage> ModifiedMessages, AIResolvedContext ResolvedContext)> PrepareContextAsync(
        IList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        // Resolve context from all registered resolvers (resolvers read from RuntimeContext)
        var resolvedContext = await _contextResolutionService.ResolveContextAsync(cancellationToken);

        // If no context resources, nothing to inject. The (empty) context is still made current for this call's
        // tools, so a call nested in another never lists the other call's resources.
        if (resolvedContext.AllResources.Count == 0)
        {
            return (messages, resolvedContext);
        }

        // Format and inject context into system prompt:
        // - "Always" resources are injected with full content
        // - "OnDemand" resources are listed so the LLM knows they're available
        var contextContent = await _contextProcessor.ProcessContextForLlmAsync(resolvedContext, cancellationToken);
        if (!string.IsNullOrWhiteSpace(contextContent))
        {
            messages = InjectContextIntoMessages(messages, contextContent);
        }

        return (messages, resolvedContext);
    }

    private static IList<ChatMessage> InjectContextIntoMessages(
        IList<ChatMessage> messages,
        string contextContent)
    {
        // Create a modifiable copy
        var modifiedMessages = messages.ToList();

        // Find the first system message, or insert one at the beginning
        var systemMessageIndex = modifiedMessages
            .Select((msg, idx) => new { Message = msg, Index = idx })
            .FirstOrDefault(x => x.Message.Role == ChatRole.System)?.Index;

        if (systemMessageIndex.HasValue)
        {
            // Append context to existing system message
            var existingMessage = modifiedMessages[systemMessageIndex.Value];
            var existingContent = existingMessage.Text ?? string.Empty;
            var newContent = string.IsNullOrWhiteSpace(existingContent)
                ? contextContent
                : $"{existingContent}\n\n{contextContent}";

            modifiedMessages[systemMessageIndex.Value] = new ChatMessage(ChatRole.System, newContent);
        }
        else
        {
            // Insert new system message at the beginning
            modifiedMessages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
        }

        return modifiedMessages;
    }
}
