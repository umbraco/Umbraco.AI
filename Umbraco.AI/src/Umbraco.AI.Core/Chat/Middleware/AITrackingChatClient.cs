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
        var descriptor = BuildDescriptor(messages);

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
        var descriptor = BuildDescriptor(messages);

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

    private AIOperationDescriptor BuildDescriptor(IReadOnlyList<ChatMessage> messages) => new()
    {
        Capability = AICapability.Chat,
        PromptData = messages,
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = true,
    };
}
