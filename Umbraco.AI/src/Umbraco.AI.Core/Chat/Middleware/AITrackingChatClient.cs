using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat client that records usage analytics and audit entries around a chat completion, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. Replaces the former separate
/// tracking/usage-recording/auditing chat client trio with a single tracker-backed client.
/// </summary>
internal sealed class AITrackingChatClient : AIBoundChatClientBase
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingChatClient(IChatClient innerClient, IAIOperationTracker tracker)
        : base(innerClient)
        => _tracker = tracker;

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
                    ResponseData = response.Messages,
                    // Same check as the streaming path: a response that ends on a provider error is a
                    // failed call, though it is still returned to the caller.
                    Failure = response.GetTerminalProviderError() is { } providerError
                        ? new AIProviderErrorContentException(providerError)
                        : null,
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

        // yield cannot sit inside try/catch, so drive the enumerator manually (matches prior behavior). The scope is
        // entered around each step: recording scopes are AsyncLocal and don't survive this iterator's yields.
        await using var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .EnterEachStep(scope.EnterScope)
            .GetAsyncEnumerator(cancellationToken);
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

        // Some providers report a failure (e.g. a rate limit hit on the final model call of a
        // tool loop) as streamed ErrorContent rather than by throwing, so the stream itself ends
        // normally. Record the call as failed when the response ends on such an error, keeping
        // the usage it consumed; an error the model carried on past stays a success.
        if (aggregated.GetTerminalProviderError() is { } providerError)
        {
            await scope.FailAsync(new AIProviderErrorContentException(providerError), aggregated.Usage);
        }
        else
        {
            await scope.CompleteAsync(
                aggregated.Usage,
                aggregated.Messages);
        }
    }

    private AIOperationDescriptor BuildDescriptor(IReadOnlyList<ChatMessage> messages) => new()
    {
        Capability = AICapability.Chat,
        PromptData = messages,
    };
}

/// <summary>
/// A provider failure reported as <see cref="ErrorContent"/> in the response, streamed or not, rather than
/// thrown. Wrapped so recorders can record it like any other failed call.
/// </summary>
internal sealed class AIProviderErrorContentException(ErrorContent error)
    : Exception(string.IsNullOrEmpty(error.ErrorCode)
        ? error.Message ?? "The provider returned an error."
        : $"{error.ErrorCode}: {error.Message ?? "The provider returned an error."}")
{
    /// <summary>
    /// Gets the provider's error code, when it sent one.
    /// </summary>
    public string? ErrorCode { get; } = error.ErrorCode;
}
