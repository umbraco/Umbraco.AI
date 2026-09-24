// DR-10 — Copilot auto mode routes with Decision
//
// AIAgentService takes decisionService and experimentalFeatures as required constructor
// parameters (placed before the optional backOfficeSecurityAccessor/loggerFactory ones). The
// class is internal, so this isn't a public-API break. The Decision gate now checks
// IAIProfileService.HasDefaultProfileAsync rather than catching an exception from
// GetDefaultProfileAsync, and the whole Decision attempt (gate + ask) falls back to the chat
// classifier on any non-cancellation exception, so a site that doesn't use Decision at all
// (e.g. a database error resolving the default profile) never breaks agent routing. Agents
// with no scope restrictions are available through the real AIAgentScopeValidator with no
// surface registered.
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tools;
using Xunit;

#pragma warning disable UMBRACOAI_DECISION

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class DecisionAgentSelectionTests
{
    private const string SurfaceId = "copilot";
    private const string UserMessage = "Help me improve this page's SEO";

    private readonly Mock<IAIAgentRepository> _repositoryMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIChatClientFactory> _chatClientFactoryMock = new();
    private readonly Mock<IChatClient> _chatClientMock = new();
    private readonly Mock<IAIDecisionService> _decisionServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalMock = new();
    private readonly Mock<ILogger> _loggerMock = new();
    private readonly List<AIAgent> _agents = [];
    private AIChoiceDecisionQuestion? _sentQuestion;

    public DecisionAgentSelectionTests()
    {
        _repositoryMock
            .Setup(r => r.GetBySurfaceAsync(SurfaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _agents);

        // Chat fallback path: classifier profile resolves and the chat model picks the first agent.
        var classifierProfile = new AIProfile
        {
            Alias = "classifier",
            Name = "Classifier",
            Capability = AICapability.Chat,
            Model = new AIModelRef("openai", "gpt-4o-mini"),
            ConnectionId = Guid.NewGuid()
        };
        _profileServiceMock.Setup(p => p.GetClassifierProfileAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classifierProfile);
        _chatClientFactoryMock.Setup(f => f.CreateClientAsync(classifierProfile, It.IsAny<CancellationToken>())).ReturnsAsync(_chatClientMock.Object);
        _chatClientMock
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, _agents[0].Id.ToString())));

        // Decision path defaults: flag on, default Decision profile exists.
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(true);
        _profileServiceMock
            .Setup(p => p.HasDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    #region Scenario: 3 agents, flag on, Decision picks agent 2

    [Fact]
    public async Task ReturnsTheAgentDecisionPicked()
    {
        GivenAgents(3);
        DecisionPicks(_agents[1].Id.ToString());

        var selected = await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        selected!.Id.ShouldBe(_agents[1].Id);
    }

    [Fact]
    public async Task MakesNoChatCall()
    {
        GivenAgents(3);
        DecisionPicks(_agents[1].Id.ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _chatClientMock.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Scenario: the Decision question describes the agents

    [Fact]
    public async Task OptionKeysAreAgentIds()
    {
        GivenAgents(3);
        DecisionPicks(_agents[0].Id.ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _sentQuestion!.Options.Select(o => o.Key).ShouldBe(_agents.Select(a => a.Id.ToString()));
    }

    [Fact]
    public async Task OptionDescriptionContainsAgentName()
    {
        GivenAgents(3);
        DecisionPicks(_agents[0].Id.ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _sentQuestion!.Options[1].Description!.ShouldContain(_agents[1].Name);
    }

    [Fact]
    public async Task OptionDescriptionContainsAgentDescription()
    {
        GivenAgents(3);
        DecisionPicks(_agents[0].Id.ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _sentQuestion!.Options[1].Description!.ShouldContain(_agents[1].Description!);
    }

    [Fact]
    public async Task ContextIsTheUserMessage()
    {
        GivenAgents(3);
        DecisionPicks(_agents[0].Id.ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _sentQuestion!.Context.ShouldBe(UserMessage);
    }

    #endregion

    #region Scenario: a single available agent

    [Fact]
    public async Task SingleAgent_IsReturned()
    {
        GivenAgents(1);

        var selected = await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        selected!.Id.ShouldBe(_agents[0].Id);
    }

    [Fact]
    public async Task SingleAgent_MakesNoDecisionCall()
    {
        GivenAgents(1);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyNoDecisionCall();
    }

    #endregion

    #region Sad path: falls back to the chat classifier

    [Fact]
    public async Task FlagOff_UsesChatPath()
    {
        GivenAgents(3);
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(false);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    [Fact]
    public async Task FlagOff_MakesNoDecisionCall()
    {
        GivenAgents(3);
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(false);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyNoDecisionCall();
    }

    [Fact]
    public async Task NoDefaultDecisionProfile_UsesChatPath()
    {
        GivenAgents(3);
        _profileServiceMock
            .Setup(p => p.HasDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    [Fact]
    public async Task DefaultProfileGateThrowsNonInvalidOperation_UsesChatPath()
    {
        GivenAgents(3);
        _profileServiceMock
            .Setup(p => p.HasDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timed out."));

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    [Fact]
    public async Task DecisionThrows_UsesChatPath()
    {
        GivenAgents(3);
        DecisionThrows();

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    [Fact]
    public async Task DecisionThrows_LogsWarning()
    {
        GivenAgents(3);
        DecisionThrows();

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        _loggerMock.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnknownKey_UsesChatPath()
    {
        GivenAgents(3);
        DecisionPicks(Guid.NewGuid().ToString());

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    [Fact]
    public async Task TooManyAgents_MakesNoDecisionCall()
    {
        GivenAgents(256);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyNoDecisionCall();
    }

    [Fact]
    public async Task TooManyAgents_UsesChatPath()
    {
        GivenAgents(256);

        await CreateService().SelectAgentForPromptAsync(UserMessage, SurfaceId, new AgentAvailabilityContext { Surface = SurfaceId });

        VerifyChatCalledOnce();
    }

    #endregion

    private void GivenAgents(int count)
    {
        _agents.Clear();
        for (var i = 0; i < count; i++)
        {
            _agents.Add(new AIAgent
            {
                Id = Guid.NewGuid(),
                Alias = $"agent-{i}",
                Name = $"Agent {i}",
                Description = $"Handles topic {i}",
                SurfaceIds = [SurfaceId],
                IsActive = true
            });
        }
    }

    private void DecisionPicks(string key)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIChoiceDecisionResponse>, CancellationToken>((_, q, _) => _sentQuestion = (AIChoiceDecisionQuestion)q)
            .ReturnsAsync(new AIChoiceDecisionResponse { Choice = key, ChoiceConfidence = 0.9 });

    private void DecisionThrows()
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Jev is down"));

    private void VerifyNoDecisionCall()
        => _decisionServiceMock.Verify(
            s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()),
            Times.Never);

    private void VerifyChatCalledOnce()
        => _chatClientMock.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);

    private AIAgentService CreateService()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        return new AIAgentService(
            _repositoryMock.Object,
            null!, // IAIEntityVersionService
            null!, // IAIAgentFactory
            null!, // IAGUIStreamingService
            null!, // IAGUIContextConverter
            null!, // IAGUIMessageConverter
            new AIToolCollection(() => []),
            _profileServiceMock.Object,
            null!, // IAIGuardrailService
            null!, // IAIContextService
            _chatClientFactoryMock.Object,
            new AIAgentScopeValidator(),
            new AIAgentSurfaceCollection(() => []),
            null!, // IEventAggregator
            decisionService: _decisionServiceMock.Object,
            experimentalFeatures: _experimentalMock.Object,
            backOfficeSecurityAccessor: null,
            loggerFactory: loggerFactory.Object);
    }
}
