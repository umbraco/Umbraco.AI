using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using Umbraco.AI.Core;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Wraps the OpenAI Responses <see cref="IChatClient"/> and, when the caller manages chat history itself
/// (<see cref="Constants.ContextKeys.ClientManagedChatHistory"/> set in the runtime context), disables the
/// Responses API's server-side storage for the call.
/// </summary>
/// <remarks>
/// <para>
/// The Responses API stores each response server-side by default and returns a conversation id.
/// Microsoft.Extensions.AI surfaces that as <see cref="ChatResponse.ConversationId"/>, which the Agent
/// Framework treats as "service-managed history" — conflicting with an attached
/// <c>ChatHistoryProvider</c> and causing the framework to detach it (so our history never persists).
/// </para>
/// <para>
/// Setting <c>StoredOutputEnabled = false</c> makes M.E.AI return a null conversation id, so the run is
/// stateless and our client-side history store remains the single source of truth. Only applied when the
/// flag is present, so ordinary OpenAI usage (contextual Copilot, prompts, etc.) is unaffected.
/// </para>
/// </remarks>
[Experimental("OPENAI001")]
internal sealed class ClientManagedHistoryChatClient : DelegatingChatClient
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;

    public ClientManagedHistoryChatClient(IChatClient innerClient, IAIRuntimeContextAccessor runtimeContextAccessor)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
    }

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, WithStatelessOutputIfNeeded(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, WithStatelessOutputIfNeeded(options), cancellationToken);

    private ChatOptions? WithStatelessOutputIfNeeded(ChatOptions? options)
    {
        var clientManaged = _runtimeContextAccessor.Context?
            .GetValue<bool>(Constants.ContextKeys.ClientManagedChatHistory) ?? false;

        if (!clientManaged)
        {
            return options;
        }

        // Clone so we never mutate the caller's options, and compose with any existing factory.
        var result = options?.Clone() ?? new ChatOptions();
        var previous = result.RawRepresentationFactory;
        result.RawRepresentationFactory = client =>
        {
            var raw = previous?.Invoke(client);
            var responseOptions = raw as CreateResponseOptions ?? new CreateResponseOptions();
            responseOptions.StoredOutputEnabled = false;
            return responseOptions;
        };

        return result;
    }
}
