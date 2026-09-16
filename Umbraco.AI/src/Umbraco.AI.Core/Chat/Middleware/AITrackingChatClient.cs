using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat client that records usage analytics and audit entries around a chat completion, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. Replaces the former separate
/// tracking/usage-recording/auditing chat client trio with a single tracker-backed client.
/// </summary>
internal sealed class AITrackingChatClient : AIBoundChatClientBase
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingChatClient(IChatClient innerClient, IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
        : base(innerClient)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var descriptor = BuildDescriptor(messages, options);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await base.GetResponseAsync(messages, options, token);
                return new AITrackedOperationResult<ChatResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    AuditResponse = new AIAuditResponse { Data = response.Messages, Usage = response.Usage },
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var descriptor = BuildDescriptor(messages, options);

        var scope = await _tracker.BeginAsync(descriptor, cancellationToken);
        var updates = new List<ChatResponseUpdate>();
        Exception? captured = null;

        // yield cannot sit inside try/catch, so drive the enumerator manually (matches prior behavior).
        await using var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                ChatResponseUpdate current;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    captured = ex;
                    break;
                }

                updates.Add(current);
                yield return current;
            }

            if (captured is not null)
            {
                await scope.FailAsync(captured);
                throw captured;
            }

            var aggregated = updates.ToChatResponse();
            await scope.CompleteAsync(
                aggregated.Usage,
                new AIAuditResponse { Data = aggregated.Messages, Usage = aggregated.Usage });
        }
        finally
        {
            scope.Dispose();
        }
    }

    private AIOperationDescriptor BuildDescriptor(IReadOnlyList<ChatMessage> messages, ChatOptions? options) => new()
    {
        Capability = AICapability.Chat,
        PromptData = WithRuntimeContextSnapshot(messages, options?.Instructions),
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = true,
    };

    /// <summary>
    /// Builds a copy of <paramref name="messages"/> for the audit log only, with the volatile
    /// runtime-context prompt -- moved to <c>ChatOptions.Instructions</c> by
    /// <c>AIAgentSystemMessageChatClient</c> so it lands after the stable, cacheable system content on
    /// the wire (see umbraco/Umbraco.AI#382) -- inserted as its own labelled entry, right after the real
    /// system message. It's context (the current entity, page, etc.), not instructions, so it's labelled
    /// that way here rather than reusing the wire field's name, which would misdescribe it to anyone
    /// reading the log. Never mutates or reuses the list actually sent to the provider.
    /// </summary>
    private static IReadOnlyList<ChatMessage> WithRuntimeContextSnapshot(IReadOnlyList<ChatMessage> messages, string? runtimeContext)
    {
        if (string.IsNullOrEmpty(runtimeContext))
        {
            return messages;
        }

        var contextMessage = new ChatMessage(new ChatRole("Context"), runtimeContext);
        var insertAt = messages.Count > 0 && messages[0].Role == ChatRole.System ? 1 : 0;

        var snapshot = messages.ToList();
        snapshot.Insert(insertAt, contextMessage);
        return snapshot;
    }
}
