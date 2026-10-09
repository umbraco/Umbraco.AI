using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

#pragma warning disable MEAI001 // Speech-to-text abstractions are experimental in M.E.AI

/// <summary>
/// The runtime context each AI entry point runs its work in, with the real scope provider: every step of a
/// streamed call sees the call's own context, and calls running at the same time never see each other's (#524).
/// </summary>
public class AIRuntimeContextFlowTests
{
    private readonly AIRuntimeContextScopeProvider _scopeProvider = new();
    private readonly AIRuntimeContextContributorCollection _contributors = new(() => []);

    [Fact]
    public async Task ScopedProfileChatClient_Streaming_EveryUpdateSeesTheProfile()
    {
        var probe = new ChatProbe(_scopeProvider, Constants.ContextKeys.ProfileAlias, updates: 3);
        var client = new ScopedProfileChatClient(probe, Profile("writer"), _scopeProvider, _scopeProvider, _contributors);

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
        }

        probe.Seen.ShouldBe(["writer", "writer", "writer"]);
        probe.SeenAtDisposal.ShouldBe("writer");
        _scopeProvider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task ScopedProfileChatClient_StreamingNestedInARunningCall_SeesItsOwnProfileAndLeavesTheParentsAlone()
    {
        using var outerScope = _scopeProvider.CreateScope();
        outerScope.Context.SetValue(Constants.ContextKeys.ProfileAlias, "outer");
        var tracker = new AIOperationTracker(_scopeProvider, [], NullLogger<AIOperationTracker>.Instance);
        var running = await tracker.BeginAsync(new AIOperationDescriptor { Capability = AICapability.Chat }, CancellationToken.None);
        var probe = new ChatProbe(_scopeProvider, Constants.ContextKeys.ProfileAlias, updates: 3);
        var client = new ScopedProfileChatClient(probe, Profile("judge"), _scopeProvider, _scopeProvider, _contributors);

        using (running.EnterScope())
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        }

        probe.Seen.ShouldBe(["judge", "judge", "judge"]);
        outerScope.Context.GetValue<string>(Constants.ContextKeys.ProfileAlias).ShouldBe("outer");
        await running.CompleteAsync(usage: null, responseData: null);
    }

    [Fact]
    public async Task ScopedProfileChatClient_Streaming_ToolsRunByFunctionInvokingSeeTheContext()
    {
        // Tools (and the tool notifications) read the runtime context while a streamed call is running.
        string? seenByTool = null;
        var tool = Microsoft.Extensions.AI.AIFunctionFactory.Create(() =>
        {
            seenByTool = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.ProfileAlias);
            return "ok";
        }, "check");
        var provider = new ToolCallingChatClient("check");
        var client = new ScopedProfileChatClient(
            provider.AsBuilder().UseFunctionInvocation().Build(),
            Profile("writer"),
            _scopeProvider,
            _scopeProvider,
            _contributors);

        await foreach (var _ in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [tool] }))
        {
        }

        provider.Requests.ShouldBe(2);
        seenByTool.ShouldBe("writer");
    }

    [Fact]
    public async Task ScopedProfileSpeechToTextClient_Streaming_EveryUpdateSeesTheProfile()
    {
        var probe = new SpeechProbe(_scopeProvider, Constants.ContextKeys.ProfileAlias, updates: 3);
        var client = new ScopedProfileSpeechToTextClient(
            probe, Profile("transcriber", AICapability.SpeechToText), _scopeProvider, _scopeProvider, _contributors);

        await foreach (var _ in client.GetStreamingTextAsync(new MemoryStream([1, 2, 3])))
        {
        }

        probe.Seen.ShouldBe(["transcriber", "transcriber", "transcriber"]);
        probe.SeenAtDisposal.ShouldBe("transcriber");
    }

    [Fact]
    public async Task ScopedInlineSpeechToTextClient_Streaming_EveryUpdateSeesItsFeature()
    {
        var probe = new SpeechProbe(_scopeProvider, Constants.ContextKeys.FeatureAlias, updates: 3);
        var client = new ScopedInlineSpeechToTextClient(
            probe, new AISpeechToTextBuilder().WithAlias("dictation"), _scopeProvider, _scopeProvider, _contributors);

        await foreach (var _ in client.GetStreamingTextAsync(new MemoryStream([1, 2, 3])))
        {
        }

        probe.Seen.ShouldBe(["dictation", "dictation", "dictation"]);
    }

    [Fact]
    public async Task ChatService_StreamChatResponse_EveryUpdateSeesTheInlineFeature()
    {
        var probe = new ChatProbe(_scopeProvider, Constants.ContextKeys.FeatureAlias, updates: 3);
        var service = CreateChatService(probe);

        await foreach (var _ in service.StreamChatResponseAsync(chat => chat.WithAlias("summary"), [new ChatMessage(ChatRole.User, "hi")]))
        {
        }

        probe.Seen.ShouldBe(["summary", "summary", "summary"]);
        _scopeProvider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task ChatService_CallsStartedTogether_EachRunInTheirOwnContext()
    {
        // #524: e.g. a developer generating several summaries at once in one request.
        var seen = new ConcurrentDictionary<string, string?>();
        var allStarted = new Barrier(3);
        var service = CreateChatService(new FakeChatClient(async (_, _, _) =>
        {
            var before = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.FeatureAlias)!;
            allStarted.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.Delay(10);
            seen[before] = _scopeProvider.Context?.GetValue<string>(Constants.ContextKeys.FeatureAlias);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
        }));

        await Task.WhenAll(new[] { "one", "two", "three" }.Select(alias => Task.Run(() =>
            service.GetChatResponseAsync(chat => chat.WithAlias(alias), [new ChatMessage(ChatRole.User, alias)]))));

        seen.Count.ShouldBe(3);
        seen.ShouldAllBe(pair => pair.Key == pair.Value);
        _scopeProvider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task ChatService_StreamsReadInTurn_EachUpdateSeesItsOwnFeature()
    {
        var probeA = new ChatProbe(_scopeProvider, Constants.ContextKeys.FeatureAlias, updates: 3);
        var probeB = new ChatProbe(_scopeProvider, Constants.ContextKeys.FeatureAlias, updates: 3);
        var serviceA = CreateChatService(probeA);
        var serviceB = CreateChatService(probeB);

        await using var a = serviceA.StreamChatResponseAsync(chat => chat.WithAlias("a"), [new ChatMessage(ChatRole.User, "a")])
            .GetAsyncEnumerator();
        await using var b = serviceB.StreamChatResponseAsync(chat => chat.WithAlias("b"), [new ChatMessage(ChatRole.User, "b")])
            .GetAsyncEnumerator();
        while (await a.MoveNextAsync() & await b.MoveNextAsync())
        {
        }

        probeA.Seen.ShouldBe(["a", "a", "a"]);
        probeB.Seen.ShouldBe(["b", "b", "b"]);
    }

    private AIChatService CreateChatService(IChatClient client)
    {
        var profile = Profile("default-chat");
        var profileService = new Mock<IAIProfileService>();
        profileService
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var clientFactory = new Mock<IAIChatClientFactory>();
        clientFactory.Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>())).ReturnsAsync(client);
        var options = new Mock<IOptionsMonitor<AIOptions>>();
        options.Setup(x => x.CurrentValue).Returns(new AIOptions { DefaultChatProfileAlias = "default-chat" });
        var events = new Mock<IEventAggregator>();
        events.Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new AIChatService(
            clientFactory.Object,
            profileService.Object,
            new Mock<IAIGuardrailService>().Object,
            new Mock<IAIContextService>().Object,
            options.Object,
            events.Object,
            _scopeProvider,
            _scopeProvider,
            _contributors,
            new AIToolCollection(() => []),
            new Umbraco.AI.Core.Tools.AIFunctionFactory(new AIToolScopeCollection(() => [])));
    }

    private static AIProfile Profile(string alias, AICapability capability = AICapability.Chat)
        => new AIProfileBuilder().WithAlias(alias).WithCapability(capability).WithModel("openai", "gpt-4").Build();

    /// <summary>Streams a call to <paramref name="toolName"/> on the first request and text on the next.</summary>
    private sealed class ToolCallingChatClient(string toolName) : IChatClient
    {
        public int Requests { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Requests++;
            await Task.Yield();
            yield return Requests == 1
                ? new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call-1", toolName)])
                : new ChatResponseUpdate(ChatRole.Assistant, "done");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>Streams chat updates, recording one runtime context value at each and when disposed.</summary>
    private sealed class ChatProbe(IAIRuntimeContextAccessor accessor, string key, int updates) : IChatClient
    {
        public List<string?> Seen { get; } = [];

        public string? SeenAtDisposal { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                for (var i = 0; i < updates; i++)
                {
                    await Task.Yield();
                    Seen.Add(accessor.Context?.GetValue<string>(key));
                    yield return new ChatResponseUpdate(ChatRole.Assistant, $"{i}");
                }
            }
            finally
            {
                SeenAtDisposal = accessor.Context?.GetValue<string>(key);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>Streams transcription updates, recording one runtime context value at each and when disposed.</summary>
    private sealed class SpeechProbe(IAIRuntimeContextAccessor accessor, string key, int updates) : ISpeechToTextClient
    {
        public List<string?> Seen { get; } = [];

        public string? SeenAtDisposal { get; private set; }

        public Task<SpeechToTextResponse> GetTextAsync(
            Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
            Stream audioSpeechStream,
            SpeechToTextOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                for (var i = 0; i < updates; i++)
                {
                    await Task.Yield();
                    Seen.Add(accessor.Context?.GetValue<string>(key));
                    yield return new SpeechToTextResponseUpdate($"{i}");
                }
            }
            finally
            {
                SeenAtDisposal = accessor.Context?.GetValue<string>(key);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
