using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIErrorClassifyingChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_OnGenericException_ThrowsClassifiedAIProviderException()
    {
        var innerClient = new FakeChatClient((_, _, _) =>
            Task.FromException<ChatResponse>(new InvalidOperationException("boom")));
        var client = CreateClient(innerClient);

        var thrown = await Should.ThrowAsync<AIProviderException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]));

        thrown.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        thrown.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task GetResponseAsync_OnRealCancellation_RethrowsUntouched()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var innerClient = new FakeChatClient((_, _, ct) =>
            Task.FromException<ChatResponse>(new OperationCanceledException(ct)));
        var client = CreateClient(innerClient);

        await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetResponseAsync_OnTimeoutManifestingAsTaskCanceled_ClassifiesAsTransient()
    {
        // HttpClient reports its own request timeout as a TaskCanceledException even though the
        // caller's token was never signalled — this must still be classified, not leaked raw.
        var innerClient = new FakeChatClient((_, _, _) =>
            Task.FromException<ChatResponse>(new TaskCanceledException("A task was canceled.", new TimeoutException())));
        var client = CreateClient(innerClient);

        var thrown = await Should.ThrowAsync<AIProviderException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], cancellationToken: CancellationToken.None));

        thrown.Category.ShouldBe(AIProviderErrorCategory.Transient);
    }

    [Fact]
    public async Task GetResponseAsync_OnAlreadyClassifiedException_PassesThroughUnwrapped()
    {
        var original = new AIProviderException(new AIProviderErrorInfo(
            AIProviderErrorCategory.RateLimited, "Rate limit reached.", "429", "raw"));
        var innerClient = new FakeChatClient((_, _, _) => Task.FromException<ChatResponse>(original));
        var client = CreateClient(innerClient);

        var thrown = await Should.ThrowAsync<AIProviderException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]));

        thrown.ShouldBeSameAs(original);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnMidStreamException_ThrowsClassifiedAIProviderException()
    {
        var innerClient = new ThrowingStreamingChatClient(new HttpRequestException("connection refused"));
        var client = CreateClient(innerClient);

        await Should.ThrowAsync<AIProviderException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        });
    }

    private static AIErrorClassifyingChatClient CreateClient(IChatClient innerClient)
        => new(innerClient, new FakeAIProvider());

    /// <summary>
    /// Minimal <see cref="IChatClient"/> that throws mid-enumeration, for exercising the manual
    /// enumerator catch in <see cref="AIErrorClassifyingChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    private sealed class ThrowingStreamingChatClient : IChatClient
    {
        private readonly Exception _exception;

        public ThrowingStreamingChatClient(Exception exception) => _exception = exception;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromException<ChatResponse>(_exception);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw _exception;
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        public ChatClientMetadata Metadata => new("ThrowingClient", null, null);
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }
}
