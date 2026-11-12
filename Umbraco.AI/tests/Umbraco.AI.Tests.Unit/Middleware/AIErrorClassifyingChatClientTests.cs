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

    [Fact]
    public async Task GetStreamingResponseAsync_OnStreamedOutOfCreditError_ThrowsOutOfCredit()
    {
        // OpenAI's Responses API reports an empty account after the HTTP 200, so it arrives as a
        // streamed error rather than an exception, and was shown to the user raw.
        var innerClient = new StreamingUpdatesChatClient(
            new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent("You have no credits remaining.") { ErrorCode = "credit_balance_exhausted" }]));
        var client = CreateClient(innerClient);

        var thrown = await Should.ThrowAsync<AIProviderException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        });

        thrown.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        thrown.ProviderCode.ShouldBe("credit_balance_exhausted");
        thrown.UserMessage.ShouldContain("out of credit");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnOutOfCreditErrorAfterText_YieldsTheTextFirst()
    {
        // A later model call in a tool loop can run out of credit after earlier text has streamed.
        var innerClient = new StreamingUpdatesChatClient(
            new ChatResponseUpdate(ChatRole.Assistant, "Looking that up."),
            new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent("You have no credits remaining.") { ErrorCode = "credit_balance_exhausted" }]));
        var client = CreateClient(innerClient);

        var texts = new List<string>();
        await Should.ThrowAsync<AIProviderException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
                texts.Add(update.Text);
            }
        });

        texts.ShouldBe(["Looking that up."]);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnOtherStreamedError_PassesItThrough()
    {
        var error = new ErrorContent("Content filtered.") { ErrorCode = "content_filter" };
        var innerClient = new StreamingUpdatesChatClient(new ChatResponseUpdate(ChatRole.Assistant, [error]));
        var client = CreateClient(innerClient);

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            updates.Add(update);
        }

        updates.SelectMany(u => u.Contents).ShouldContain(error);
    }

    [Fact]
    public async Task GetResponseAsync_OnResponseEndingInOutOfCreditError_ThrowsOutOfCredit()
    {
        var innerClient = new FakeChatClient((_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, [new ErrorContent("You exceeded your quota.") { ErrorCode = "insufficient_quota" }]))));
        var client = CreateClient(innerClient);

        var thrown = await Should.ThrowAsync<AIProviderException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]));

        thrown.UserMessage.ShouldContain("out of credit");
    }

    private static AIErrorClassifyingChatClient CreateClient(IChatClient innerClient)
        => new(innerClient, new FakeAIProvider());

    /// <summary>
    /// Minimal <see cref="IChatClient"/> that streams the given updates.
    /// </summary>
    private sealed class StreamingUpdatesChatClient(params ChatResponseUpdate[] updates) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(updates.ToChatResponse());

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

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
