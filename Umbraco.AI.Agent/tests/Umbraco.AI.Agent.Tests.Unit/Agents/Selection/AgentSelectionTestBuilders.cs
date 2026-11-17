// Builders for the agent-selection specs that compile today. Everything here is unconditional - it
// exercises code that already exists on this branch (through T9: the selection types, the selection
// service, LLMAgentSelector, StickyAgentSelector, the obsolete SelectAgentForPromptAsync proxy and its
// builder registration). AgentSelectionTestHarness.cs holds only the T10 (StreamAgentAGUIController
// wiring) pieces behind its own #if - once that lands, fold what's left there in here and delete that file.
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agents.Selection;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;
using CoreConstants = Umbraco.AI.Core.Constants;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents.Selection;

/// <summary>
/// Serialises specs that swap <see cref="StaticServiceProvider.Instance"/> (currently only
/// <see cref="AgentSelectionTestBuilders.AgentServiceHarness"/>) against each other, so a test on one
/// thread can't observe - or stomp on - another test's swapped-in provider. xUnit never runs test
/// classes in the same collection in parallel with each other.
/// </summary>
[CollectionDefinition(Name)]
public sealed class StaticServiceProviderCollection
{
    public const string Name = "StaticServiceProvider";
}

/// <summary>Builders shared by agent-selection specs that only need today's real types.</summary>
internal static class AgentSelectionTestBuilders
{
    public const string SurfaceId = "copilot";

    public sealed class TestSurface : IAIAgentSurface
    {
        public string Id => SurfaceId;
        public string Icon => "icon-robot";
        public IReadOnlyList<string> SupportedScopeDimensions => ["section", "entityType"];
    }

    /// <summary>
    /// A Copilot agent. <paramref name="allowedSection"/> null means "no scope rules". Enough to drive
    /// both <see cref="AIAgentService"/> and <see cref="AIAgentSelectionService"/>.
    /// </summary>
    public static UmbracoAIAgent CreateAgent(
        Guid id,
        string alias,
        bool isActive = true,
        string? allowedSection = null,
        string? description = null)
    {
        return new UmbracoAIAgent
        {
            Id = id,
            Alias = alias,
            Name = $"{alias} name",
            Description = description,
            AgentType = AIAgentType.Standard,
            IsActive = isActive,
            SurfaceIds = [SurfaceId],
            Scope = allowedSection is null
                ? null
                : new AIAgentScope { AllowRules = [new AIAgentScopeRule { Sections = [allowedSection] }] },
        };
    }

    public static AgentAvailabilityContext CreateAvailabilityContext(string section = "content", string? entityType = null)
        => new() { Surface = SurfaceId, Section = section, EntityType = entityType };

    public static AIAgentSelectionInput CreateInput(
        string section = "content",
        string? entityType = null,
        IReadOnlyList<ChatMessage>? messages = null,
        IReadOnlyList<AIRequestContextItem>? contextItems = null,
        IReadOnlyList<AIFrontendTool>? frontendTools = null,
        Guid? previousAgentId = null)
        => new()
        {
            SurfaceId = SurfaceId,
            AvailabilityContext = CreateAvailabilityContext(section, entityType),
            Messages = messages ?? [new ChatMessage(ChatRole.User, "hello")],
            ContextItems = contextItems ?? [],
            FrontendTools = frontendTools ?? [],
            PreviousAgentId = previousAgentId,
        };

    /// <summary>A request handed straight to a selector, bypassing the service.</summary>
    public static AIAgentSelectionRequest CreateRequest(
        IReadOnlyList<UmbracoAIAgent> candidates,
        IReadOnlyList<ChatMessage>? messages = null,
        UmbracoAIAgent? previousAgent = null)
        => new()
        {
            CandidateAgents = candidates,
            Messages = messages ?? [new ChatMessage(ChatRole.User, "hello")],
            AvailabilityContext = CreateAvailabilityContext(),
            ContextItems = [],
            SurfaceId = SurfaceId,
            UserGroupIds = [],
            PreviousAgent = previousAgent,
        };

    public static AIFrontendTool CreateFrontendTool(string name)
        => new(new AGUITool { Name = name, Description = name }, Scope: null, IsDestructive: false);

    public static IBackOfficeSecurityAccessor CreateBackOfficeSecurityAccessor(IReadOnlyList<Guid> userGroupIds)
    {
        var groups = userGroupIds
            .Select(id => Mock.Of<IReadOnlyUserGroup>(g => g.Key == id))
            .ToList();

        var user = new Mock<IUser>();
        user.Setup(x => x.Groups).Returns(groups);

        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);

        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);
        return accessor.Object;
    }

    /// <summary>A selector whose answer is scripted, and which records every request it sees.</summary>
    public sealed class RecordingSelector : IAIAgentSelector
    {
        private readonly Func<AIAgentSelectionRequest, AIAgentSelectionResult?> _decide;

        public RecordingSelector(Func<AIAgentSelectionRequest, AIAgentSelectionResult?> decide) => _decide = decide;

        public List<AIAgentSelectionRequest> Requests { get; } = [];

        public Task<AIAgentSelectionResult?> SelectAgentAsync(
            AIAgentSelectionRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_decide(request));
        }

        public static RecordingSelector Returning(UmbracoAIAgent? agent, string selectorId = "test", string? reason = null)
            => new(_ => agent is null ? null : new AIAgentSelectionResult(agent, selectorId, reason));

        public static RecordingSelector Throwing(Exception exception)
            => new(_ => throw exception);
    }

    /// <summary>
    /// A real <see cref="AIAgentSelectionService"/> over mocked collaborators. <see cref="IAIAgentService"/>
    /// is mocked just far enough to answer <see cref="IAIAgentService.GetAgentsBySurfaceAsync"/> - scope
    /// filtering, the chain, and the fallback all run for real.
    /// </summary>
    public sealed class SelectionServiceBuilder
    {
        private readonly List<UmbracoAIAgent> _agents = [];
        private readonly List<IAIAgentSelector> _selectors = [];
        private readonly List<Guid> _userGroupIds = [];

        public SelectionServiceBuilder WithAgents(params UmbracoAIAgent[] agents)
        {
            _agents.AddRange(agents);
            return this;
        }

        public SelectionServiceBuilder WithSelectors(params IAIAgentSelector[] selectors)
        {
            _selectors.AddRange(selectors);
            return this;
        }

        public SelectionServiceBuilder WithUserGroups(params Guid[] userGroupIds)
        {
            _userGroupIds.AddRange(userGroupIds);
            return this;
        }

        private readonly List<AIAgentSelectedNotification> _publishedNotifications = [];

        /// <summary>Every <see cref="AIAgentSelectedNotification"/> published by the built service, in order.</summary>
        public IReadOnlyList<AIAgentSelectedNotification> PublishedNotifications => _publishedNotifications;

        public IAIAgentSelectionService Build()
        {
            var agentService = new Mock<IAIAgentService>();
            agentService
                .Setup(x => x.GetAgentsBySurfaceAsync(SurfaceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_agents);

            var eventAggregator = new Mock<IEventAggregator>();
            eventAggregator
                .Setup(x => x.PublishAsync(It.IsAny<AIAgentSelectedNotification>(), It.IsAny<CancellationToken>()))
                .Callback<AIAgentSelectedNotification, CancellationToken>((n, _) => _publishedNotifications.Add(n))
                .Returns(Task.CompletedTask);

            return new AIAgentSelectionService(
                agentService.Object,
                new AIAgentSurfaceCollection(() => [new TestSurface()]),
                new AIAgentScopeValidator(),
                new AIAgentSelectorCollection(() => _selectors),
                eventAggregator.Object,
                NullLogger<AIAgentSelectionService>.Instance,
                CreateBackOfficeSecurityAccessor(_userGroupIds));
        }
    }

    /// <summary>
    /// A real <see cref="LLMAgentSelector"/> whose classifier replies with <paramref name="classifierReply"/>.
    /// A null reply means "no classifier or default chat profile configured".
    /// </summary>
    public static (LLMAgentSelector Selector, List<IList<ChatMessage>> SentPrompts) CreateLLMSelector(string? classifierReply)
    {
        var sentPrompts = new List<IList<ChatMessage>>();
        var profileService = new Mock<IAIProfileService>();
        var chatClientFactory = new Mock<IAIChatClientFactory>();

        if (classifierReply is null)
        {
            profileService
                .Setup(x => x.GetClassifierProfileAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("No classifier or default chat profile."));
        }
        else
        {
            var profile = new AIProfile { Alias = "classifier", Name = "Classifier", ConnectionId = Guid.NewGuid() };
            profileService
                .Setup(x => x.GetClassifierProfileAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            var chatClient = new Mock<IChatClient>();
            chatClient
                .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((m, _, _) => sentPrompts.Add(m.ToList()))
                .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, classifierReply)));

            chatClientFactory
                .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(chatClient.Object);
        }

        return (new LLMAgentSelector(profileService.Object, chatClientFactory.Object), sentPrompts);
    }

    /// <summary>
    /// A real <see cref="AIAgentService"/>, built from today's constructor (no selection-service
    /// parameter - that lands in a later task), wired just far enough to run the AG-UI options
    /// overload of <see cref="AIAgentService.StreamAgentAGUIAsync(Guid, AGUIRunRequest, IEnumerable{AIFrontendTool}?, AIAgentExecutionOptions, CancellationToken)"/>
    /// and capture the additional properties handed to the agent factory.
    /// </summary>
    public sealed class AgentServiceAuditHarness
    {
        public AgentServiceAuditHarness(UmbracoAIAgent agent)
        {
            var repository = new Mock<IAIAgentRepository>();
            repository.Setup(x => x.GetByIdAsync(agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

            var messageConverter = new Mock<IAGUIMessageConverter>();
            messageConverter.Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>())).Returns([]);

            var contextConverter = new Mock<IAGUIContextConverter>();
            contextConverter
                .Setup(x => x.ConvertToRequestContextItems(It.IsAny<IEnumerable<AGUIContextItem>>()))
                .Returns([]);

            var agentFactory = new Mock<IAIAgentFactory>();
            agentFactory
                .Setup(x => x.CreateAgentAsync(
                    It.IsAny<UmbracoAIAgent>(),
                    It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                    It.IsAny<IEnumerable<AITool>?>(),
                    It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                    It.IsAny<AIApprovalPolicy>(),
                    It.IsAny<CancellationToken>()))
                .Callback<UmbracoAIAgent, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, AIApprovalPolicy, CancellationToken>(
                    (_, _, _, props, _, _) => AdditionalProperties = props)
                .ReturnsAsync(new Mock<MsAIAgent>().Object);

            // AIAgentService.StreamAgentAGUIAsync calls the session-aware overload (added for Copilot
            // Workspace's persisted conversations, umbraco/Umbraco.AI#375) even for a null historyBinding,
            // so the mock must answer that overload, not the simpler 4-arg one (an unmatched call returns
            // a null IAsyncEnumerable, which the `await foreach` then NREs on).
            var streamingService = new Mock<IAGUIStreamingService>();
            streamingService
                .Setup(x => x.StreamAgentAsync(
                    It.IsAny<MsAIAgent>(),
                    It.IsAny<AGUIRunRequest>(),
                    It.IsAny<IEnumerable<AITool>?>(),
                    It.IsAny<AgentSession?>(),
                    It.IsAny<IReadOnlyDictionary<string, ToolApprovalRequestContent>?>(),
                    It.IsAny<IReadOnlyList<ToolApprovalRequestContent>?>(),
                    It.IsAny<AIConversationPersistenceSync?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(EmptyEventStream());

            Service = new AIAgentService(
                repository.Object,
                null!, // IAIEntityVersionService
                agentFactory.Object,
                streamingService.Object,
                contextConverter.Object,
                messageConverter.Object,
                new AIToolCollection(() => []),
                null!, // IAIProfileService
                null!, // IAIGuardrailService
                null!, // IAIContextService
                Mock.Of<IEventAggregator>(),
                backOfficeSecurityAccessor: null);
        }

        public AIAgentService Service { get; }

        public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; private set; }

        public IReadOnlyList<string> LogKeys
            => AdditionalProperties?.TryGetValue(CoreConstants.ContextKeys.LogKeys, out var keys) == true
                ? (string[])keys!
                : [];

        public async Task RunAsync(AIAgentExecutionOptions options, Guid agentId)
        {
            var request = new AGUIRunRequest { ThreadId = "thread", RunId = "run", Messages = [] };
            await foreach (var _ in Service.StreamAgentAGUIAsync(agentId, request, frontendTools: null, options))
            {
            }
        }
    }

    /// <summary>
    /// A real <see cref="AIAgentService"/>, built from today's constructor (it does not take
    /// <see cref="IAIAgentSelectionService"/> - see the remarks on
    /// <see cref="AIAgentService.SelectAgentForPromptAsync"/> for why), used to exercise the obsolete
    /// <c>SelectAgentForPromptAsync</c> proxy.
    /// </summary>
    /// <remarks>
    /// The proxy resolves <see cref="IAIAgentSelectionService"/> from
    /// <see cref="StaticServiceProvider"/>, so the constructor swaps <see cref="StaticServiceProvider.Instance"/>
    /// in for <paramref name="selectionService"/> and <see cref="Dispose"/> restores whatever was there
    /// before. Specs using this harness must dispose it, and must be in the
    /// <see cref="StaticServiceProviderCollection"/> test collection so no other test can observe (or
    /// clobber) the swapped-in instance while this one is live.
    /// </remarks>
    public sealed class AgentServiceHarness : IDisposable
    {
        private readonly IServiceProvider _previousServiceProvider;

        public AgentServiceHarness(UmbracoAIAgent agent, IAIAgentSelectionService? selectionService = null)
        {
            // Captured before anything else so a throw below - from this constructor's own setup or
            // from AIAgentService's - always has a provider to restore. Without this, a failure after
            // the swap below would leak the swapped-in provider to every other test, since Dispose()
            // is never called on an object whose constructor didn't complete.
            _previousServiceProvider = StaticServiceProvider.Instance;
            try
            {
                var repository = new Mock<IAIAgentRepository>();
                repository.Setup(x => x.GetByIdAsync(agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

                Service = new AIAgentService(
                    repository.Object,
                    null!, // IAIEntityVersionService
                    null!, // IAIAgentFactory
                    null!, // IAGUIStreamingService
                    null!, // IAGUIContextConverter
                    null!, // IAGUIMessageConverter
                    new AIToolCollection(() => []),
                    null!, // IAIProfileService
                    null!, // IAIGuardrailService
                    null!, // IAIContextService
                    Mock.Of<IEventAggregator>(),
                    backOfficeSecurityAccessor: null);

                var services = new ServiceCollection();
                services.AddSingleton(selectionService ?? Mock.Of<IAIAgentSelectionService>());

                StaticServiceProvider.Instance = services.BuildServiceProvider();
            }
            catch
            {
                StaticServiceProvider.Instance = _previousServiceProvider;
                throw;
            }
        }

        public AIAgentService Service { get; }

        public void Dispose() => StaticServiceProvider.Instance = _previousServiceProvider;
    }

    public static async IAsyncEnumerable<IAGUIEvent> EmptyEventStream()
    {
        await Task.CompletedTask;
        yield break;
    }
}
