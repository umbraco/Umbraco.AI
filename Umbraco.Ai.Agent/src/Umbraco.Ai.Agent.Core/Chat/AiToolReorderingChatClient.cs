using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// A chat client that reorders tool calls in responses to ensure server-side tools
/// are processed before frontend tools.
/// </summary>
/// <remarks>
/// <para>
/// This client solves the problem of mixed frontend and server-side tool execution.
/// When the model calls multiple tools, <see cref="FunctionInvokingChatClient"/> processes
/// them in the order they appear. If a frontend tool (which sets <c>Terminate = true</c>)
/// appears first, server-side tools never execute.
/// </para>
/// <para>
/// This wrapper reorders <see cref="FunctionCallContent"/> items in responses so that:
/// <list type="number">
///   <item>Server-side tools appear first and execute</item>
///   <item>Frontend tools appear last and trigger termination</item>
///   <item>All server-side results are collected before the loop stops</item>
/// </list>
/// </para>
/// <para>
/// Frontend tool names are read from <see cref="AiRuntimeContext"/>
/// using the key <see cref="Constants.ContextKeys.FrontendToolNames"/>. This allows
/// the middleware to be stateless and frontend tools to be specified per-request.
/// </para>
/// </remarks>
internal sealed class AiToolReorderingChatClient : DelegatingChatClient
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiToolReorderingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client (typically the provider).</param>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    public AiToolReorderingChatClient(IChatClient innerClient, IAiRuntimeContextAccessor runtimeContextAccessor)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var frontendToolNames = GetFrontendToolNames();
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);

        // Reorder tool calls in the response message if we have frontend tools
        if (frontendToolNames.Count > 0 && response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Any())
        {
            response = ReorderToolCallsInResponse(response, frontendToolNames);
        }

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var frontendToolNames = GetFrontendToolNames();

        // If no frontend tools, pass through without collecting
        if (frontendToolNames.Count == 0)
        {
            await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // For streaming with frontend tools, we need to collect all content first,
        // then reorder and re-yield. This is because tool calls may come in any order.
        var collectedUpdates = new List<ChatResponseUpdate>();
        var hasToolCalls = false;

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            collectedUpdates.Add(update);

            if (update.Contents?.OfType<FunctionCallContent>().Any() == true)
            {
                hasToolCalls = true;
            }
        }

        // If no tool calls, just yield everything as-is
        if (!hasToolCalls)
        {
            foreach (var update in collectedUpdates)
            {
                yield return update;
            }
            yield break;
        }

        // Reorder: yield non-tool-call updates first, then reordered tool calls
        var toolCallUpdates = new List<ChatResponseUpdate>();
        var otherUpdates = new List<ChatResponseUpdate>();

        foreach (var update in collectedUpdates)
        {
            if (update.Contents?.OfType<FunctionCallContent>().Any() == true)
            {
                toolCallUpdates.Add(update);
            }
            else
            {
                otherUpdates.Add(update);
            }
        }

        // Yield non-tool updates first (text content, etc.)
        foreach (var update in otherUpdates)
        {
            yield return update;
        }

        // Reorder tool call updates: server-side first, frontend last
        var reorderedToolUpdates = toolCallUpdates
            .OrderBy(u => IsFrontendToolCall(u, frontendToolNames) ? 1 : 0)
            .ToList();

        foreach (var update in reorderedToolUpdates)
        {
            yield return update;
        }
    }

    private HashSet<string> GetFrontendToolNames()
    {
        return _runtimeContextAccessor.Context?.TryGetValue<string[]>(Constants.ContextKeys.FrontendToolNames, out var names) == true 
            ? new HashSet<string>(names, StringComparer.OrdinalIgnoreCase) 
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static ChatResponse ReorderToolCallsInResponse(
        ChatResponse response,
        HashSet<string> frontendToolNames)
    {
        // Get all messages and find those with tool calls
        var messages = response.Messages.ToList();
        var reorderedMessages = new List<ChatMessage>();
        var anyOrderChanged = false;

        foreach (var message in messages)
        {
            var contents = message.Contents.ToList();
            var toolCalls = contents.OfType<FunctionCallContent>().ToList();

            if (toolCalls.Count <= 1)
            {
                // No reordering needed for this message
                reorderedMessages.Add(message);
                continue;
            }

            var otherContent = contents.Where(c => c is not FunctionCallContent).ToList();

            // Reorder: server-side tools first, frontend tools last
            var reorderedToolCalls = toolCalls
                .OrderBy(tc => frontendToolNames.Contains(tc.Name) ? 1 : 0)
                .ToList();

            // Check if order actually changed
            var orderChanged = !toolCalls.SequenceEqual(reorderedToolCalls);
            if (!orderChanged)
            {
                reorderedMessages.Add(message);
                continue;
            }

            anyOrderChanged = true;

            // Build new content list: other content first, then reordered tool calls
            var newContents = new List<AIContent>();
            newContents.AddRange(otherContent);
            newContents.AddRange(reorderedToolCalls);

            // Create new message with reordered contents
            var newMessage = new ChatMessage(message.Role, newContents);
            reorderedMessages.Add(newMessage);
        }

        if (!anyOrderChanged)
        {
            return response;
        }

        // Create new response with the reordered messages
        return new ChatResponse(reorderedMessages)
        {
            ResponseId = response.ResponseId,
            ModelId = response.ModelId,
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason,
            Usage = response.Usage,
            RawRepresentation = response.RawRepresentation,
            AdditionalProperties = response.AdditionalProperties
        };
    }

    private static bool IsFrontendToolCall(ChatResponseUpdate update, HashSet<string> frontendToolNames)
    {
        var toolCall = update.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
        return toolCall != null && frontendToolNames.Contains(toolCall.Name);
    }
}
