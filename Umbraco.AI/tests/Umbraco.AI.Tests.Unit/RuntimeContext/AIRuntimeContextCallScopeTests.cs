using Microsoft.AspNetCore.Http;
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
    private readonly AIRuntimeContextScopeProvider _scopeProvider = new(Mock.Of<IHttpContextAccessor>());
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
    public async Task Begin_AfterTheRunningCallEnded_UsesTheCallersContext()
    {
        using var outerScope = _scopeProvider.CreateScope([]);
        var running = await _tracker.BeginAsync(Descriptor(), CancellationToken.None);

        using (running.EnterScope())
        {
            await running.CompleteAsync(usage: null, responseData: null);

            using var scope = Begin([]);

            scope.ShouldBeNull();
        }
    }

    private IAIRuntimeContextScope? Begin(IEnumerable<AIRequestContextItem> items)
        => AIRuntimeContextCallScope.Begin(_scopeProvider, _scopeProvider, _contributors, items);

    private static AIRequestContextItem Item(string description, string value) => new() { Description = description, Value = value };

    private static AIOperationDescriptor Descriptor() => new() { Capability = AICapability.Chat };

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
