using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IChatClient"/> for use in tests.
/// </summary>
public class FakeChatClient : IChatClient
{
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? _getResponseHandler;
    private readonly string _defaultResponse;
    private readonly IReadOnlyList<string>? _streamingChunks;
    private readonly ChatFinishReason? _streamingFinishReason;

    public FakeChatClient(string defaultResponse = "This is a fake response.")
    {
        _defaultResponse = defaultResponse;
    }

    public FakeChatClient(Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> getResponseHandler)
    {
        _getResponseHandler = getResponseHandler;
        _defaultResponse = string.Empty;
    }

    /// <summary>
    /// Creates a fake client that streams exactly the given chunks, in order, with no re-splitting —
    /// use this when a test needs to control precisely where a chunk boundary falls (e.g. splitting a
    /// matched phrase across two chunks).
    /// </summary>
    public FakeChatClient(IReadOnlyList<string> streamingChunks, ChatFinishReason? streamingFinishReason = null)
    {
        _streamingChunks = streamingChunks;
        _streamingFinishReason = streamingFinishReason;
        _defaultResponse = string.Concat(streamingChunks);
    }

    /// <summary>
    /// Gets the list of messages that were sent to this client.
    /// </summary>
    public List<IEnumerable<ChatMessage>> ReceivedMessages { get; } = [];

    /// <summary>
    /// Gets the list of options that were sent to this client.
    /// </summary>
    public List<ChatOptions?> ReceivedOptions { get; } = [];

    public ChatClientMetadata Metadata { get; } = new("FakeChatClient", new Uri("https://fake.test"), "fake-model");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedMessages.Add(chatMessages);
        ReceivedOptions.Add(options);

        if (_getResponseHandler is not null)
        {
            return await _getResponseHandler(chatMessages, options, cancellationToken);
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, _defaultResponse));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ReceivedMessages.Add(chatMessages);
        ReceivedOptions.Add(options);

        if (_streamingChunks is not null)
        {
            for (var i = 0; i < _streamingChunks.Count; i++)
            {
                await Task.Delay(1, cancellationToken);

                var isLast = i == _streamingChunks.Count - 1;
                var update = new ChatResponseUpdate(ChatRole.Assistant, _streamingChunks[i]);
                if (isLast && _streamingFinishReason is not null)
                {
                    update.FinishReason = _streamingFinishReason;
                }

                yield return update;
            }

            yield break;
        }

        // Simulate streaming by yielding word by word
        var words = _defaultResponse.Split(' ');
        foreach (var word in words)
        {
            await Task.Delay(1, cancellationToken); // Small delay to simulate streaming
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
        }
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(IChatClient) || serviceType == typeof(FakeChatClient))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
