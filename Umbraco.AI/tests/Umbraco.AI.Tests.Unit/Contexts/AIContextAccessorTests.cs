using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Middleware;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Context;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Contexts;

/// <summary>
/// The resolved context the on-demand context tools read: each AI call's tools see that call's resources, even
/// when calls run at the same time or one runs inside another, and with no HTTP request.
/// </summary>
public class AIContextAccessorTests
{
    private readonly AIContextAccessor _accessor = new();

    [Fact]
    public void SetContext_MakesItCurrent_AndDisposingRestoresThePreviousOne()
    {
        var outer = Context("outer");
        var inner = Context("inner");

        using (_accessor.SetContext(outer))
        {
            using (_accessor.SetContext(inner))
            {
                _accessor.Context.ShouldBeSameAs(inner);
            }

            // A nested call's disposal no longer clears its parent's context.
            _accessor.Context.ShouldBeSameAs(outer);
        }

        _accessor.Context.ShouldBeNull();
    }

    [Fact]
    public async Task CallsRunningTogether_EachSeeOnlyTheirOwnContext()
    {
        var bothSet = new Barrier(2);

        async Task<bool> RunAsync(string name)
        {
            await Task.Yield();
            var context = Context(name);
            using var _ = _accessor.SetContext(context);
            bothSet.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.Delay(10);
            return ReferenceEquals(_accessor.Context, context);
        }

        var results = await Task.WhenAll(Task.Run(() => RunAsync("a")), Task.Run(() => RunAsync("b")));

        results.ShouldAllBe(ok => ok);
    }

    [Fact]
    public async Task OneCallFinishing_DoesNotClearAnotherCallsContext()
    {
        var bSet = new TaskCompletionSource();
        var aFinished = new TaskCompletionSource();
        var contextB = Context("b");

        var a = Task.Run(async () =>
        {
            using (_accessor.SetContext(Context("a")))
            {
                await bSet.Task;
            }

            aFinished.SetResult();
        });
        var b = Task.Run(async () =>
        {
            using var _ = _accessor.SetContext(contextB);
            bSet.SetResult();
            await aFinished.Task;
            return _accessor.Context;
        });

        await a;
        (await b).ShouldBeSameAs(contextB);
    }

    [Fact]
    public async Task InjectingClient_Streaming_ToolsRunOnLaterStepsSeeTheCallsResources()
    {
        // Tools run after a tool call, on a later step of the stream than the one that set the context.
        var listTool = new ListContextResourcesTool(_accessor);
        object? toolResult = null;
        var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(async () =>
        {
            toolResult = await ((IAITool)listTool).ExecuteAsync(null);
            return "ok";
        }, "list_context_resources");
        var provider = new ToolCallingChatClient("list_context_resources");
        var client = Injecting(provider.AsBuilder().UseFunctionInvocation().Build(), Context("brand-guide"));

        await foreach (var _ in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [function] }))
        {
        }

        provider.Requests.ShouldBe(2);
        toolResult.ShouldBeOfType<ListContextResourcesResult>().Resources.Select(r => r.Name).ShouldBe(["brand-guide"]);
        _accessor.Context.ShouldBeNull();
    }

    [Fact]
    public async Task InjectingClient_NonStreaming_ToolsSeeTheCallsResources()
    {
        var listTool = new ListContextResourcesTool(_accessor);
        object? toolResult = null;
        var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(async () =>
        {
            toolResult = await ((IAITool)listTool).ExecuteAsync(null);
            return "ok";
        }, "list_context_resources");
        var provider = new ToolCallingChatClient("list_context_resources");
        var client = Injecting(provider.AsBuilder().UseFunctionInvocation().Build(), Context("brand-guide"));

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [function] });

        toolResult.ShouldBeOfType<ListContextResourcesResult>().Resources.Select(r => r.Name).ShouldBe(["brand-guide"]);
    }

    [Fact]
    public async Task InjectingClient_NestedCallWithNoResources_ListsNoneAndLeavesTheOuterCallsContext()
    {
        // A tool of the outer call makes a nested AI call (with no resources of its own) whose tools list resources,
        // then the outer call's tool lists them again.
        var outerContext = Context("outer-guide");
        var listTool = new ListContextResourcesTool(_accessor);
        object? nestedList = null;
        object? outerListAfterNested = null;
        var nestedClient = Injecting(new FakeChatClient(async (_, _, _) =>
        {
            nestedList = await ((IAITool)listTool).ExecuteAsync(null);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
        }), AIResolvedContext.Empty);
        var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(async () =>
        {
            await nestedClient.GetResponseAsync([new ChatMessage(ChatRole.User, "nested")]);
            outerListAfterNested = await ((IAITool)listTool).ExecuteAsync(null);
            return "ok";
        }, "run_nested");
        var provider = new ToolCallingChatClient("run_nested");
        var outerClient = Injecting(provider.AsBuilder().UseFunctionInvocation().Build(), outerContext);

        await foreach (var _ in outerClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [function] }))
        {
        }

        nestedList.ShouldBeOfType<ListContextResourcesResult>().Resources.ShouldBeEmpty();
        outerListAfterNested.ShouldBeOfType<ListContextResourcesResult>().Resources.Select(r => r.Name)
            .ShouldBe(["outer-guide"]);
    }

    [Fact]
    public async Task InjectingClient_StreamsReadInTurn_EachToolSeesItsOwnCallsResources()
    {
        var seen = new ConcurrentDictionary<string, string>();
        IChatClient Call(string name)
        {
            var listTool = new ListContextResourcesTool(_accessor);
            var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(async () =>
            {
                var result = (ListContextResourcesResult)await ((IAITool)listTool).ExecuteAsync(null);
                seen[name] = string.Join(",", result.Resources.Select(r => r.Name));
                return "ok";
            }, "list_context_resources");
            var inner = new ToolCallingChatClient("list_context_resources").AsBuilder().UseFunctionInvocation().Build();
            return new OptionsAddingChatClient(Injecting(inner, Context(name)), new ChatOptions { Tools = [function] });
        }

        await using var a = Call("a").GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "a")]).GetAsyncEnumerator();
        await using var b = Call("b").GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "b")]).GetAsyncEnumerator();
        while (await a.MoveNextAsync() & await b.MoveNextAsync())
        {
        }

        seen["a"].ShouldBe("a");
        seen["b"].ShouldBe("b");
    }

    [Fact]
    public async Task ManyCallsTogether_EachToolSeesOnlyItsOwnCallsResources()
    {
        // Stress: 50 streamed calls with a tool call each, all at once.
        var failures = 0;

        async Task CallAsync(int i)
        {
            var name = $"call-{i}";
            var listTool = new ListContextResourcesTool(_accessor);
            var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(async () =>
            {
                await Task.Delay(Random.Shared.Next(0, 3));
                var result = (ListContextResourcesResult)await ((IAITool)listTool).ExecuteAsync(null);
                if (result.Resources.Count != 1 || result.Resources[0].Name != name)
                {
                    Interlocked.Increment(ref failures);
                }

                return "ok";
            }, "list_context_resources");
            var inner = new ToolCallingChatClient("list_context_resources").AsBuilder().UseFunctionInvocation().Build();
            await foreach (var _ in Injecting(inner, Context(name)).GetStreamingResponseAsync(
                [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [function] }))
            {
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 50).Select(i => Task.Run(() => CallAsync(i))));

        failures.ShouldBe(0);
    }

    private AIContextInjectingChatClient Injecting(IChatClient inner, AIResolvedContext resolved)
    {
        var resolution = new Mock<IAIContextResolutionService>();
        resolution.Setup(x => x.ResolveContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(resolved);
        var processor = new Mock<IAIContextProcessor>();
        processor.Setup(x => x.ProcessContextForLlmAsync(It.IsAny<AIResolvedContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        return new AIContextInjectingChatClient(inner, resolution.Object, processor.Object, _accessor);
    }

    private static AIResolvedContext Context(string resourceName)
    {
        var resource = new AIResolvedResource
        {
            Id = Guid.NewGuid(),
            ResourceTypeId = "text",
            Name = resourceName,
            InjectionMode = AIContextResourceInjectionMode.OnDemand,
            Source = "test",
            ContextName = "Test context",
        };

        return new AIResolvedContext { OnDemandResources = [resource], AllResources = [resource] };
    }

    /// <summary>Adds fixed options to every request, so a stream can be started with no options.</summary>
    private sealed class OptionsAddingChatClient(IChatClient inner, ChatOptions options) : DelegatingChatClient(inner)
    {
        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? _ = null, CancellationToken cancellationToken = default)
            => base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    /// <summary>Asks for <paramref name="toolName"/> on the first request and answers with text on the next.</summary>
    private sealed class ToolCallingChatClient(string toolName) : IChatClient
    {
        private int _requests;

        public int Requests => _requests;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Interlocked.Increment(ref _requests) == 1
                ? new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", toolName)]))
                : new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var first = Interlocked.Increment(ref _requests) == 1;
            await Task.Yield();
            yield return first
                ? new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call-1", toolName)])
                : new ChatResponseUpdate(ChatRole.Assistant, "done");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
