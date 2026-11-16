// S7 - Workspace auto mode uses my selection rules (AC1-AC7)
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream.Controllers;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agents.Selection;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Stream;

/// <summary>
/// Copilot Workspace's per-conversation stream endpoint runs <c>auto</c> conversations through the
/// pluggable selection service, like the plain Copilot endpoint, with the previous pick taken from the
/// newest assistant message's agent.
/// </summary>
/// <remarks>
/// Assumed seams (T15 may adjust them in <see cref="Harness"/> only):
/// <list type="bullet">
/// <item>The controller's new public constructor is
/// <c>(conversationService, projectService, agentService, selectionService, messageConverter,
/// toolConverter, historyProvider)</c>; the old one is <c>[Obsolete]</c>.</item>
/// <item><c>IAIConversationService.GetLastAssistantAgentIdAsync(Guid, CancellationToken)</c> (T14).</item>
/// <item><see cref="ConversationChatHistoryProvider"/>'s internal constructor takes an
/// <see cref="IAIRuntimeContextAccessor"/> after the file store (T14).</item>
/// </list>
/// </remarks>
public class StreamConversationAutoSelectionTests
{
    private static readonly UmbracoAIAgent AgentA = CreateAgent("aaaaaaaa-0000-0000-0000-000000000001", "agent-a");
    private static readonly UmbracoAIAgent AgentB = CreateAgent("bbbbbbbb-0000-0000-0000-000000000002", "agent-b");

    public class GivenAnAutoConversationAndASelectorThatPicksAgentB
    {
        private readonly Harness _harness;
        private readonly IResult _result;

        public GivenAnAutoConversationAndASelectorThatPicksAgentB()
        {
            _harness = new Harness(
                agentIdOrAlias: "auto",
                selection: new AIAgentSelectionResult(AgentB, "my-rule", "because"));
            _result = _harness.StreamAsync().GetAwaiter().GetResult();
        }

        // AC1
        [Fact]
        public void RunsAgentB() => _harness.StreamedAgentId.ShouldBe(AgentB.Id);

        [Fact]
        public void SelectsForTheWorkspaceSurface()
            => _harness.CapturedInput!.SurfaceId.ShouldBe(CopilotWorkspaceAgentSurface.SurfaceId);

        // AC3
        [Fact]
        public async Task StartsTheStreamWithAgentSelectedCarryingTheSelectorId()
            => (await Harness.ReadFirstEventValueAsync(_result)).GetProperty("selectorId").GetString().ShouldBe("my-rule");

        // AC4
        [Fact]
        public void PassesTheSelectionToTheRun()
            => _harness.CapturedOptions!.Selection!.SelectorId.ShouldBe("my-rule");

        [Fact]
        public void StillBindsTheConversationHistory()
            => _harness.CapturedOptions!.ConversationHistory.ShouldNotBeNull();
    }

    public class GivenAnAutoConversationWhoseLastReplyWasByAgentA
    {
        private readonly Harness _harness;

        public GivenAnAutoConversationWhoseLastReplyWasByAgentA()
        {
            _harness = new Harness(
                agentIdOrAlias: "auto",
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Sticky, null),
                lastAssistantAgentId: AgentA.Id);
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        // AC5
        [Fact]
        public void PassesAgentAAsThePreviousPick() => _harness.CapturedInput!.PreviousAgentId.ShouldBe(AgentA.Id);
    }

    public class GivenANewAutoConversationWithNoReplies
    {
        private readonly Harness _harness;

        public GivenANewAutoConversationWithNoReplies()
        {
            _harness = new Harness(
                agentIdOrAlias: null,
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Llm, null),
                lastAssistantAgentId: null);
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        // AC6
        [Fact]
        public void PassesNoPreviousPick() => _harness.CapturedInput!.PreviousAgentId.ShouldBeNull();
    }

    public class GivenARegenerateWithNoInboundUserMessage
    {
        private readonly Harness _harness;

        public GivenARegenerateWithNoInboundUserMessage()
        {
            _harness = new Harness(
                agentIdOrAlias: "auto",
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Llm, null),
                lastUserMessageText: "summarise this page");
            _harness.StreamAsync(withUserMessage: false).GetAwaiter().GetResult();
        }

        // SPEC Copilot Workspace 2 (regenerate falls back to the last persisted user text, as today)
        [Fact]
        public void SelectsOnTheLastPersistedUserMessage()
            => _harness.CapturedInput!.Messages.Last(m => m.Role == ChatRole.User).Text.ShouldBe("summarise this page");
    }

    public class GivenAnExplicitActiveAgent
    {
        private readonly Harness _harness;

        public GivenAnExplicitActiveAgent()
        {
            _harness = new Harness(agentIdOrAlias: AgentA.Id.ToString(), selection: null, explicitAgent: AgentA);
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        // AC2
        [Fact]
        public void RunsTheExplicitAgent() => _harness.StreamedAgentId.ShouldBe(AgentA.Id);

        // AC2
        [Fact]
        public void RunsNoSelection()
            => _harness.SelectionService.Verify(
                x => x.SelectAgentAsync(It.IsAny<AIAgentSelectionInput>(), It.IsAny<CancellationToken>()),
                Times.Never);

        // SPEC Copilot Workspace 1
        [Fact]
        public void PassesNoSelectionToTheRun() => _harness.CapturedOptions!.Selection.ShouldBeNull();
    }

    public class GivenANamedAgentThatIsMissing
    {
        private readonly Harness _harness;

        public GivenANamedAgentThatIsMissing()
        {
            // No explicitAgent is wired up, so GetAgentAsync for this id resolves to null - the same
            // "missing" outcome a deleted or inactive named agent produces (SPEC "Copilot Workspace" 1-2:
            // TryGetExplicitActiveAgentAsync returns null for either, falling through to auto-selection).
            _harness = new Harness(
                agentIdOrAlias: Guid.NewGuid().ToString(),
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Llm, null));
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        // SPEC Copilot Workspace 1-2
        [Fact]
        public void FallsBackToSelection()
            => _harness.SelectionService.Verify(
                x => x.SelectAgentAsync(It.IsAny<AIAgentSelectionInput>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    public class GivenANamedAgentNotOptedInToWorkspace
    {
        private static readonly UmbracoAIAgent OptedOut =
            CreateAgent("cccccccc-0000-0000-0000-000000000003", "agent-c", surfaceIds: ["copilot"]);

        private readonly Harness _harness;

        public GivenANamedAgentNotOptedInToWorkspace()
        {
            // The stored agent exists and is active, but is only opted in to the sidebar Copilot - it
            // is treated like a missing/inactive one and the run falls back to auto-selection.
            _harness = new Harness(
                agentIdOrAlias: OptedOut.Id.ToString(),
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Llm, null),
                explicitAgent: OptedOut);
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public void FallsBackToSelection()
            => _harness.SelectionService.Verify(
                x => x.SelectAgentAsync(It.IsAny<AIAgentSelectionInput>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact]
        public void RunsTheSelectedAgentInstead() => _harness.StreamedAgentId.ShouldBe(AgentA.Id);
    }

    public class GivenANamedAgentThatIsInactive
    {
        private static readonly UmbracoAIAgent Inactive =
            CreateAgent("dddddddd-0000-0000-0000-000000000004", "agent-d", isActive: false);

        private readonly Harness _harness;

        public GivenANamedAgentThatIsInactive()
        {
            _harness = new Harness(
                agentIdOrAlias: Inactive.Id.ToString(),
                selection: new AIAgentSelectionResult(AgentA, AIAgentSelectorIds.Llm, null),
                explicitAgent: Inactive);
            _harness.StreamAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public void RunsTheSelectedAgentInstead() => _harness.StreamedAgentId.ShouldBe(AgentA.Id);
    }

    public class GivenAClientThatSendsItsOwnThreadId
    {
        private readonly Harness _harness;

        public GivenAClientThatSendsItsOwnThreadId()
        {
            _harness = new Harness(agentIdOrAlias: AgentA.Id.ToString(), selection: null, explicitAgent: AgentA);
            _harness.StreamAsync(clientThreadId: Guid.NewGuid().ToString()).GetAwaiter().GetResult();
        }

        // #748: files are stored under the run's threadId but rehydrated, purged and kept alive by
        // conversation id, so the run must use the conversation id whatever the client sends.
        [Fact]
        public void RunsUnderTheConversationIdAsThreadId()
            => _harness.CapturedRequest!.ThreadId.ShouldBe(_harness.ConversationId.ToString());
    }

    public class GivenAnAutoConversationAndNoAvailableAgents
    {
        private readonly IResult _result;

        public GivenAnAutoConversationAndNoAvailableAgents()
            => _result = new Harness(agentIdOrAlias: "auto", selection: null).StreamAsync().GetAwaiter().GetResult();

        // AC7
        [Fact]
        public void Returns404NoAgentAvailable()
            => _result.ShouldBeOfType<NotFound<ProblemDetails>>().Value!.Title.ShouldBe("No agent available");
    }

    // AIAgent.Id has an internal setter (Umbraco.AI.Agent.Core doesn't grant this assembly
    // InternalsVisibleTo), so it's set via reflection here - the same workaround
    // Umbraco.AI.Automate's tests use for the same cross-package constraint.
    private static UmbracoAIAgent CreateAgent(
        string id,
        string alias,
        bool isActive = true,
        IReadOnlyList<string>? surfaceIds = null)
    {
        var agent = new UmbracoAIAgent
        {
            Alias = alias,
            Name = alias,
            AgentType = AIAgentType.Standard,
            IsActive = isActive,
            SurfaceIds = surfaceIds ?? [CopilotWorkspaceAgentSurface.SurfaceId],
        };

        typeof(UmbracoAIAgent).GetProperty(nameof(UmbracoAIAgent.Id), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(agent, Guid.Parse(id));

        return agent;
    }

    /// <summary>
    /// A real <see cref="StreamConversationAGUIController"/>. Conversation, project, agent and selection
    /// services are mocked; the history provider is real (it is only bound, never run).
    /// </summary>
    private sealed class Harness
    {
        private readonly Guid _conversationId = Guid.NewGuid();
        private readonly StreamConversationAGUIController _controller;

        public Harness(
            string? agentIdOrAlias,
            AIAgentSelectionResult? selection,
            UmbracoAIAgent? explicitAgent = null,
            Guid? lastAssistantAgentId = null,
            string? lastUserMessageText = null)
        {
            var conversationService = new Mock<IAIConversationService>();
            conversationService
                .Setup(x => x.GetConversationAsync(_conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIConversation { Id = _conversationId, AgentIdOrAlias = agentIdOrAlias });
            conversationService
                .Setup(x => x.GetLastAssistantAgentIdAsync(_conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lastAssistantAgentId);
            conversationService
                .Setup(x => x.GetLastUserMessageTextAsync(_conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lastUserMessageText);

            if (explicitAgent is not null)
            {
                AgentService
                    .Setup(x => x.GetAgentAsync(explicitAgent.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(explicitAgent);
            }

            AgentService
                .Setup(x => x.StreamAgentAGUIAsync(
                    It.IsAny<Guid>(), It.IsAny<AGUIRunRequest>(), It.IsAny<IEnumerable<AIFrontendTool>?>(),
                    It.IsAny<AIAgentExecutionOptions>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, AGUIRunRequest, IEnumerable<AIFrontendTool>?, AIAgentExecutionOptions, CancellationToken>(
                    (id, request, _, options, _) =>
                    {
                        StreamedAgentId = id;
                        CapturedRequest = request;
                        CapturedOptions = options;
                    })
                .Returns(EmptyEventStream());

            SelectionService
                .Setup(x => x.SelectAgentAsync(It.IsAny<AIAgentSelectionInput>(), It.IsAny<CancellationToken>()))
                .Callback<AIAgentSelectionInput, CancellationToken>((input, _) => CapturedInput = input)
                .ReturnsAsync(selection);

            // Mirrors AGUIMessageConverter for plain-text messages: one ChatMessage per AG-UI message.
            var messageConverter = new Mock<IAGUIMessageConverter>();
            messageConverter
                .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
                .Returns<IEnumerable<AGUIMessage>?>(messages => (messages ?? [])
                    .Select(m => new ChatMessage(m.Role == AGUIMessageRole.User ? ChatRole.User : ChatRole.Assistant, m.Content ?? string.Empty))
                    .ToList());

            var toolConverter = new Mock<IAGUIToolConverter>();
            toolConverter
                .Setup(x => x.ConvertToFrontendTools(It.IsAny<IEnumerable<AGUITool>?>()))
                .Returns([]);

            var historyProvider = new ConversationChatHistoryProvider(
                Mock.Of<IAIConversationRepository>(),
                Mock.Of<IAIFileStore>(),
                Mock.Of<IAIRuntimeContextAccessor>(),
                NullLogger<ConversationChatHistoryProvider>.Instance,
                Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures.Repairer(Mock.Of<IAIConversationRepository>()));

            _controller = new StreamConversationAGUIController(
                conversationService.Object,
                Mock.Of<IAIProjectService>(),
                AgentService.Object,
                SelectionService.Object,
                messageConverter.Object,
                toolConverter.Object,
                historyProvider,
                new AIAgentScopeValidator(),
                new AIAgentSurfaceCollection(() => [new CopilotWorkspaceAgentSurface()]));
        }

        public Mock<IAIAgentService> AgentService { get; } = new();

        public Mock<IAIAgentSelectionService> SelectionService { get; } = new();

        public AIAgentSelectionInput? CapturedInput { get; private set; }

        public AIAgentExecutionOptions? CapturedOptions { get; private set; }

        public Guid? StreamedAgentId { get; private set; }

        public AGUIRunRequest? CapturedRequest { get; private set; }

        public Guid ConversationId => _conversationId;

        public Task<IResult> StreamAsync(bool withUserMessage = true, string? clientThreadId = null)
            => _controller.StreamAgentAGUI(
                _conversationId,
                new AGUIRunRequest
                {
                    ThreadId = clientThreadId ?? _conversationId.ToString(),
                    RunId = "run",
                    Messages = withUserMessage
                        ? [new AGUIMessage { Id = "m1", Role = AGUIMessageRole.User, Content = "hello" }]
                        : [],
                });

        public static async Task<JsonElement> ReadFirstEventValueAsync(IResult result)
        {
            var context = new DefaultHttpContext();
            await using var body = new MemoryStream();
            context.Response.Body = body;

            await result.ExecuteAsync(context);

            var text = Encoding.UTF8.GetString(body.ToArray());
            var firstData = text
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .First(chunk => chunk.StartsWith("data: ", StringComparison.Ordinal))["data: ".Length..];

            return JsonDocument.Parse(firstData).RootElement.GetProperty("value").Clone();
        }

        private static async IAsyncEnumerable<IAGUIEvent> EmptyEventStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
