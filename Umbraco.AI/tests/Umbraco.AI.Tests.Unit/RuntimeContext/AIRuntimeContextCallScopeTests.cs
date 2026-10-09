using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

/// <summary>
/// An AI call made inside another one while that call's context is current (a guardrail judge, a tool that
/// calls an AI service) gets its own runtime context, so the two calls' settings don't leak into each other.
/// </summary>
public class AIRuntimeContextCallScopeTests
{
    private readonly AIRuntimeContextScopeProvider _scopeProvider = new();
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly AIOperationTracker _tracker;

    public AIRuntimeContextCallScopeTests()
    {
        _contributors = new AIRuntimeContextContributorCollection(() => [new SectionContributor()]);
        _tracker = new AIOperationTracker(_scopeProvider, [], NullLogger<AIOperationTracker>.Instance);
    }

    [Fact]
    public void Begin_WithNoContext_CreatesAndPopulatesOne()
    {
        using var scope = Begin([Item("section", "content")]);

        scope.ShouldNotBeNull();
        _scopeProvider.Context.ShouldBeSameAs(scope.Context);
        scope.Context.GetValue<string>(Constants.ContextKeys.Section).ShouldBe("content");
    }

    [Fact]
    public void Begin_InACallersContext_WithNoCallRunning_UsesIt()
    {
        using var callerScope = _scopeProvider.CreateScope([]);

        using var scope = Begin([]);

        scope.ShouldBeNull();
        _scopeProvider.Context.ShouldBeSameAs(callerScope.Context);
    }

    [Fact]
    public async Task Begin_InTheContextOfARunningCall_GetsItsOwnContextWithTheRunningCallsIdentityOnly()
    {
        // Arrange: a running call whose context carries its identity and its own settings.
        using var outerScope = _scopeProvider.CreateScope([Item("section", "content")]);
        var outer = outerScope.Context;
        _contributors.Populate(outer);
        outer.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions { Tools = [] });
        outer.SetValue(Constants.ContextKeys.GuardrailIdsOverride, new List<Guid> { Guid.NewGuid() });
        outer.SetValue(Constants.ContextKeys.FeatureType, "agent");
        outer.SetValue(Constants.ContextKeys.EntityId, "1234");
        outer.SetValue(Constants.ContextKeys.LogKeys, new[] { "Agent.RunId" });
        outer.SetValue("Agent.RunId", "run-1");
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        // Act
        using (running.EnterScope())
        {
            using var scope = Begin([]);

            // Assert: same request and identity, none of the running call's settings.
            scope.ShouldNotBeNull();
            var context = scope.Context;
            context.ShouldNotBeSameAs(outer);
            context.GetValue<string>(Constants.ContextKeys.Section).ShouldBe("content");
            context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe("agent");
            context.GetValue<string>(Constants.ContextKeys.EntityId).ShouldBe("1234");
            context.GetLogValues().ShouldNotBeNull()["Agent.RunId"].ShouldBe("run-1");
            context.GetValue<ChatOptions>(Constants.ContextKeys.ChatOptionsOverride).ShouldBeNull();
            context.Data.ContainsKey(Constants.ContextKeys.GuardrailIdsOverride).ShouldBeFalse();

            // Its writes stay its own.
            context.SetValue(Constants.ContextKeys.FeatureType, "inline-chat");
            outer.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe("agent");
        }

        _scopeProvider.Context.ShouldBeSameAs(outer);
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task InlineChat_NestedInARunningCall_DoesNotGetTheRunningCallsOptionsOverride()
    {
        // A guardrail judge run inside a prompt call: the prompt's tools must not reach the judge.
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "x", "list_context_resources")],
        });
        outerScope.Context.SetValue(Constants.ContextKeys.FeatureType, "prompt");
        var inner = new FakeChatClient();
        var judge = new ScopedInlineChatClient(
            new AIChatOptionsOverrideChatClient(inner, _scopeProvider),
            new AIChatBuilder().WithAlias("guardrail-llm-evaluator"),
            _scopeProvider,
            _scopeProvider,
            _contributors);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            await judge.GetResponseAsync([new ChatMessage(ChatRole.User, "Is this safe?")]);
        }

        inner.ReceivedOptions[0]?.Tools.ShouldBeNull();
        outerScope.Context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe("prompt");
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task PassThroughInlineChat_NestedInARunningCall_KeepsTheRunningCallsFeature()
    {
        // e.g. an agent's tool making a pass-through AI call: it is recorded under the agent.
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(Constants.ContextKeys.FeatureType, "agent");
        string? featureSeen = null;
        var inner = new FakeChatClient((_, _, _) =>
        {
            featureSeen = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.FeatureType);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        });
        var tool = new ScopedInlineChatClient(
            inner,
            new AIChatBuilder().WithAlias("tool-call").AsPassThrough(),
            _scopeProvider,
            _scopeProvider,
            _contributors);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            await tool.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]);
        }

        featureSeen.ShouldBe("agent");
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task Begin_ForAGuardrailEvaluation_KeepsTheEvaluationFlag()
    {
        using var outerScope = _scopeProvider.CreateScope([]);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            outerScope.Context.SetValue(Constants.ContextKeys.IsGuardrailEvaluation, true);

            using var scope = Begin([]);

            scope.ShouldNotBeNull().Context.GetValue<bool>(Constants.ContextKeys.IsGuardrailEvaluation).ShouldBeTrue();
        }

        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task Begin_InAContextACallerSetUpInsideARunningCall_UsesIt()
    {
        // A tool running inside an agent call runs a prompt: the prompt service sets up the prompt's own
        // context, which its AI call must use as it is.
        using var outerScope = _scopeProvider.CreateScope([]);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            using var promptScope = _scopeProvider.CreateScope([]);
            promptScope.Context.SetValue(Constants.ContextKeys.FeatureType, "prompt");

            using var scope = Begin([]);

            scope.ShouldBeNull();
            _scopeProvider.Context.ShouldBeSameAs(promptScope.Context);
        }

        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task Begin_InWorkThatOutlivesItsCall_GetsItsOwnContextWithTheCallsIdentity()
    {
        // Work a call started that is still running after the call finished is still that call's work.
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(Constants.ContextKeys.FeatureType, "prompt");
        outerScope.Context.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions());
        var call = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (call.EnterScope())
        {
            await call.CompleteAsync(usage: null, responseData: null);

            using var scope = Begin([]);

            scope.ShouldNotBeNull();
            scope.Context.ShouldNotBeSameAs(outerScope.Context);
            scope.Context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe("prompt");
            scope.Context.GetValue<ChatOptions>(Constants.ContextKeys.ChatOptionsOverride).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Begin_AfterACallReturned_InTheCallersOwnCode_UsesTheCallersContext()
    {
        // e.g. the prompt service retrying: its first call is over and no longer current, so the retry runs in
        // the context the prompt service set up.
        using var outerScope = _scopeProvider.CreateScope([]);
        var call = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);
        using (call.EnterScope())
        {
        }

        await call.CompleteAsync(usage: null, responseData: null);

        using var scope = Begin([]);

        scope.ShouldBeNull();
        _scopeProvider.Context.ShouldBeSameAs(outerScope.Context);
    }

    [Fact]
    public async Task BackgroundWork_ThatMakesAnAICallAfterItsCallFinished_RunsInItsOwnContext()
    {
        // A tool starts background work that makes an AI call once the agent call that started it has finished
        // and its caller has disposed the agent's scope.
        var agentScope = _scopeProvider.CreateScope([]);
        agentScope.Context.SetValue(Constants.ContextKeys.FeatureType, "agent");
        agentScope.Context.SetValue(Constants.ContextKeys.ChatOptionsOverride, new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "x", "agent_tool")],
        });
        var agentCall = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);
        var agentFinished = new TaskCompletionSource();
        AIRuntimeContext? seenByLateCall = null;
        ChatOptions? optionsSentByLateCall = null;
        var inner = new FakeChatClient((_, options, _) =>
        {
            seenByLateCall = _scopeProvider.Context;
            optionsSentByLateCall = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        });
        var lateCall = new ScopedInlineChatClient(
            new AIChatOptionsOverrideChatClient(inner, _scopeProvider),
            Builder("late-call"),
            _scopeProvider,
            _scopeProvider,
            _contributors);

        Task background;
        using (agentCall.EnterScope())
        {
            background = Task.Run(async () =>
            {
                await agentFinished.Task;
                await lateCall.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);
            });
        }

        await agentCall.CompleteAsync(usage: null, responseData: null);
        agentScope.Dispose();
        agentFinished.SetResult();
        await background;

        seenByLateCall.ShouldNotBeNull().ShouldNotBeSameAs(agentScope.Context);
        seenByLateCall.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe("agent");
        seenByLateCall.GetValue<string>(CallKey).ShouldBe("late-call");
        optionsSentByLateCall?.Tools.ShouldBeNull();
        agentScope.Context.GetValue<string>(CallKey).ShouldBeNull();
    }

    [Fact]
    public async Task ParallelNestedCalls_EachSeeOnlyTheirOwnContext_AndLeaveTheRunningCallsAlone()
    {
        // #524: two AI calls made together inside a running call (e.g. tools run concurrently).
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(CallKey, "outer");
        var bothStarted = new Barrier(2);
        var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, (string? Alias, AIRuntimeContext? Context)>();
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        ScopedInlineChatClient Judge(string alias) => new(
            new FakeChatClient(async (_, _, _) =>
            {
                bothStarted.SignalAndWait(TimeSpan.FromSeconds(5));
                await Task.Delay(10);
                var context = _scopeProvider.Context;
                seen[alias] = (context?.GetValue<string>(CallKey), context);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
            }),
            Builder(alias),
            _scopeProvider,
            _scopeProvider,
            _contributors);

        using (running.EnterScope())
        {
            await Task.WhenAll(
                Task.Run(() => Judge("judge-a").GetResponseAsync([new ChatMessage(ChatRole.User, "a")])),
                Task.Run(() => Judge("judge-b").GetResponseAsync([new ChatMessage(ChatRole.User, "b")])));
        }

        seen["judge-a"].Alias.ShouldBe("judge-a");
        seen["judge-b"].Alias.ShouldBe("judge-b");
        seen["judge-a"].Context.ShouldNotBeSameAs(seen["judge-b"].Context);
        outerScope.Context.GetValue<string>(CallKey).ShouldBe("outer");
        _scopeProvider.Context.ShouldBeSameAs(outerScope.Context);
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task ManyParallelNestedCalls_NeverSeeEachOthersContext()
    {
        // Stress: 100 nested calls inside one running call, overlapping at random.
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(CallKey, "outer");
        var failures = 0;
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        async Task CallAsync(int i)
        {
            var alias = $"call-{i}";
            var client = new ScopedInlineChatClient(
                new FakeChatClient(async (_, _, _) =>
                {
                    await Task.Delay(Random.Shared.Next(0, 5));
                    if (_scopeProvider.Context?.GetValue<string>(CallKey) != alias)
                    {
                        Interlocked.Increment(ref failures);
                    }

                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
                }),
                Builder(alias),
                _scopeProvider,
                _scopeProvider,
                _contributors);

            await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);
        }

        using (running.EnterScope())
        {
            await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(() => CallAsync(i))));
        }

        failures.ShouldBe(0);
        outerScope.Context.GetValue<string>(CallKey).ShouldBe("outer");
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task StreamedNestedCall_SeesItsOwnContextOnEveryUpdate()
    {
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(CallKey, "outer");
        var probe = new StreamingProbeClient(_scopeProvider, updates: 3);
        var client = new ScopedInlineChatClient(
            probe, Builder("streamed"), _scopeProvider, _scopeProvider, _contributors);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        }

        probe.AliasesSeen.ShouldBe(["streamed", "streamed", "streamed"]);
        probe.AliasAtDisposal.ShouldBe("streamed");
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task TwoStreamedNestedCallsReadInTurn_EachUpdateSeesItsOwnContext()
    {
        using var outerScope = _scopeProvider.CreateScope([]);
        outerScope.Context.SetValue(CallKey, "outer");
        var probeA = new StreamingProbeClient(_scopeProvider, updates: 3);
        var probeB = new StreamingProbeClient(_scopeProvider, updates: 3);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            await using var a = new ScopedInlineChatClient(
                    probeA, Builder("a"), _scopeProvider, _scopeProvider, _contributors)
                .GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "a")]).GetAsyncEnumerator();
            await using var b = new ScopedInlineChatClient(
                    probeB, Builder("b"), _scopeProvider, _scopeProvider, _contributors)
                .GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "b")]).GetAsyncEnumerator();

            while (await a.MoveNextAsync() & await b.MoveNextAsync())
            {
            }
        }

        probeA.AliasesSeen.ShouldBe(["a", "a", "a"]);
        probeB.AliasesSeen.ShouldBe(["b", "b", "b"]);
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task StreamedTopLevelCall_SeesItsContextOnEveryUpdate()
    {
        // No context exists: the streaming client creates one inside its iterator.
        var probe = new StreamingProbeClient(_scopeProvider, updates: 3);
        var client = new ScopedInlineChatClient(
            probe, Builder("top-level"), _scopeProvider, _scopeProvider, _contributors);

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            _scopeProvider.Context.ShouldBeNull();
        }

        probe.AliasesSeen.ShouldBe(["top-level", "top-level", "top-level"]);
        _scopeProvider.Context.ShouldBeNull();
    }

    private const string CallKey = "Test.Call";

    /// <summary>An inline chat that marks its own context with <paramref name="call"/>.</summary>
    private static AIChatBuilder Builder(string call)
        => new AIChatBuilder().WithAlias(call).WithAdditionalProperties(new Dictionary<string, object?> { [CallKey] = call });

    private IAIRuntimeContextScope? Begin(IEnumerable<AIRequestContextItem> items)
        => AIRuntimeContextCallScope.Begin(_scopeProvider, _scopeProvider, _contributors, items);

    private static AIRequestContextItem Item(string description, string value) => new() { Description = description, Value = value };

    private static AIOperationDescriptor Descriptor() => new() { Capability = AICapability.Chat };

    /// <summary>Streams updates, recording the feature alias of the current context at each one.</summary>
    private sealed class StreamingProbeClient(IAIRuntimeContextAccessor accessor, int updates) : IChatClient
    {
        public List<string?> AliasesSeen { get; } = [];

        public string? AliasAtDisposal { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                for (var i = 0; i < updates; i++)
                {
                    await Task.Yield();
                    AliasesSeen.Add(accessor.Context?.GetValue<string>(CallKey));
                    yield return new ChatResponseUpdate(ChatRole.Assistant, $"{i}");
                }
            }
            finally
            {
                AliasAtDisposal = accessor.Context?.GetValue<string>(CallKey);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>Copies the "section" request item into the context, like the real section contributor.</summary>
    private sealed class SectionContributor : IAIRuntimeContextContributor
    {
        public void Contribute(AIRuntimeContext context)
        {
            var section = context.RequestContextItems.FirstOrDefault(i => i.Description == "section");
            if (section is not null)
            {
                context.SetValue(Constants.ContextKeys.Section, section.Value);
            }
        }
    }
}
