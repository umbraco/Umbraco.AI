using Microsoft.Extensions.AI;

namespace Umbraco.AI.Amazon.Tests.Unit.Fakes;

/// <summary>
/// A minimal <see cref="IChatClient"/> that records the <see cref="ChatOptions"/> it was called with,
/// so tests can assert on what a decorator forwarded to the underlying provider client.
/// </summary>
internal sealed class RecordingChatClient : IChatClient
{
    public ChatOptions? ReceivedOptions { get; private set; }

    public bool WasCalled { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedOptions = options;
        WasCalled = true;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ReceivedOptions = options;
        WasCalled = true;
        yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
