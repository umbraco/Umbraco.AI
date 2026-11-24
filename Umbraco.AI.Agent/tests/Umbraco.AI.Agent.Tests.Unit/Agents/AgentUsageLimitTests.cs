using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using Xunit;

using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// IAIAgentService hides and refuses limited agents for users outside their groups.
/// </summary>
public class AgentUsageLimitTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task List_ForMember_IncludesLimitedAgent()
    {
        // Given an agent limited to editors and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When agents are listed
        var listed = await harness.Service.GetAgentsAsync();

        // Then the agent is included (GP-9 AC1)
        listed.ShouldContain(limited);
    }

    [Fact]
    public async Task Run_ForMember_StartsTheRun()
    {
        // Given an agent limited to editors and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When RunAgentAsync is called
        var response = await harness.Service.RunAgentAsync(limited.Id, Messages);

        // Then the run starts (GP-10 AC1)
        response.Text.ShouldBe("ok");
    }

    [Fact]
    public async Task List_ForMemberOfOneOfSeveralGroups_IncludesLimitedAgent()
    {
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors, Translators]);

        var listed = await harness.Service.GetAgentsAsync();

        listed.ShouldContain(limited);
    }

    [Fact]
    public async Task List_ForAnyone_IncludesAgentWithNoLimit()
    {
        var harness = new Harness(inGroups: []);
        var open = harness.Add("open", allowedGroups: []);

        var listed = await harness.Service.GetAgentsAsync();

        listed.ShouldContain(open);
    }

    [Fact]
    public async Task GetByAlias_ForManagerWhoAsksForManagedAccess_ReturnsLimitedAgent()
    {
        // Given a user with Uai.Agent.Manage not in editors
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManageAgents]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When managed access is asked for
        var found = await harness.Service.GetAgentByAliasAsync("limited", includeRestricted: true);

        // Then the agent is returned (GP-9 AC2)
        found.ShouldBe(limited);
    }

    [Fact]
    public async Task Paged_ForManagerWhoAsksForManagedAccess_ListsLimitedAgent()
    {
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManageAgents]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        var (items, total) = await harness.Service.GetAgentsPagedAsync(0, 10, null, null, null, null, null, includeRestricted: true);

        items.ShouldContain(limited);
        total.ShouldBe(1);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task List_ForNonMember_ExcludesLimitedAgent()
    {
        // Given an agent limited to editors and a user not in it
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);
        var open = harness.Add("open", allowedGroups: []);

        // When agents are listed
        var listed = await harness.Service.GetAgentsAsync();

        // Then the agent isn't included (GP-9 AC3)
        listed.ShouldNotContain(limited);
        listed.ShouldContain(open);
    }

    [Fact]
    public async Task Paged_ForNonMember_ExcludesLimitedAgentFromItemsAndTotal()
    {
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);
        var open = harness.Add("open", allowedGroups: []);

        var (items, total) = await harness.Service.GetAgentsPagedAsync(0, 10);

        items.ShouldBe([open]);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task Paged_ForNonMemberWithoutManage_IgnoresIncludeRestricted()
    {
        // GP-9 AC4
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);

        var (items, _) = await harness.Service.GetAgentsPagedAsync(0, 10, null, null, null, null, null, includeRestricted: true);

        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task BySurface_ForNonMember_ExcludesLimitedAgent()
    {
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors], surfaces: ["copilot"]);

        var listed = await harness.Service.GetAgentsBySurfaceAsync("copilot");

        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByAlias_ForNonMemberWithoutManage_ReturnsNull()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);

        // When GetAgentByAliasAsync is called for the limited agent
        var found = await harness.Service.GetAgentByAliasAsync("limited");

        // Then it returns null (GP-9 AC5)
        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_ForNonMemberWithoutManage_ReturnsNull()
    {
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        var found = await harness.Service.GetAgentAsync(limited.Id);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetByAlias_ForNonMemberWithoutManage_IgnoresIncludeRestricted()
    {
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);

        var found = await harness.Service.GetAgentByAliasAsync("limited", includeRestricted: true);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_ForManagerWhoDidNotAskForManagedAccess_ReturnsNull()
    {
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManageAgents]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        var found = await harness.Service.GetAgentAsync(limited.Id);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task List_ForUsableButOutOfScopeAgent_ExcludesIt()
    {
        // Given an agent the user may use but out of scope for the surface
        var harness = new Harness(inGroups: [Translators]);
        var usableInScope = harness.Add("in-scope", allowedGroups: [], surfaces: ["copilot"]);
        harness.Add("out-of-scope", allowedGroups: [], surfaces: ["copilot"], scope: new AIAgentScope
        {
            AllowRules = [new AIAgentScopeRule { Sections = ["media"] }],
        });
        harness.Add("limited", allowedGroups: [Editors], surfaces: ["copilot"]);

        // When the surface lists agents (the list the selection service builds, then scope rules)
        var surface = new Mock<IAIAgentSurface>();
        surface.SetupGet(x => x.SupportedScopeDimensions).Returns(["section"]);
        var context = new AgentAvailabilityContext { Surface = "copilot", Section = "content" };
        var validator = new AIAgentScopeValidator();
        var listed = (await harness.Service.GetAgentsBySurfaceAsync("copilot"))
            .Where(a => validator.IsAgentAvailable(a, context, surface.Object))
            .ToList();

        // Then only the agent that is both usable and in scope is listed (GP-9 AC7, R13)
        listed.ShouldBe([usableInScope]);
    }

    public static TheoryData<RunMethod> RunMethods => new()
    {
        RunMethod.RunById,
        RunMethod.RunByAlias,
        RunMethod.StreamById,
        RunMethod.StreamByAlias,
        RunMethod.StreamAGUI,
    };

    [Theory]
    [MemberData(nameof(RunMethods))]
    public async Task Run_ForNonMember_IsRefused(RunMethod method)
    {
        // Given an agent limited to editors and a user not in it
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When any run or stream method is called
        var run = () => harness.InvokeAsync(method, limited);

        // Then an agent-not-available failure is returned (GP-10 AC2)
        var failure = await Should.ThrowAsync<AIAgentNotAvailableException>(run);
        failure.AgentId.ShouldBe(limited.Id);
    }

    [Theory]
    [MemberData(nameof(RunMethods))]
    public async Task Run_ForManagerOutsideGroups_IsRefused(RunMethod method)
    {
        // Given a user with Uai.Agent.Manage not in editors
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManageAgents]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When they run the agent limited to editors
        var run = () => harness.InvokeAsync(method, limited);

        // Then an agent-not-available failure is returned (GP-10 AC3)
        await Should.ThrowAsync<AIAgentNotAvailableException>(run);
    }

    [Fact]
    public async Task Run_WithNoUserAndNoExplicitGroups_IsRefused()
    {
        // Headless caller with no group list: fail closed.
        var harness = new Harness(inGroups: null);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        await Should.ThrowAsync<AIAgentNotAvailableException>(() => harness.Service.RunAgentAsync(limited.Id, Messages));
    }

    [Fact]
    public async Task Run_WithExplicitGroups_UsesThemInsteadOfTheCurrentUsersGroups()
    {
        // The signed-in user isn't in editors, but the caller (Automate's service account) says its groups are.
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        var response = await harness.Service.RunAgentAsync(
            limited.Id, Messages, new AIAgentExecutionOptions { UserGroupIds = [Editors] });

        response.Text.ShouldBe("ok");
    }

    [Fact]
    public async Task Run_WithExplicitGroupsOutsideTheLimit_IsRefusedEvenIfTheCurrentUserIsInIt()
    {
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        await Should.ThrowAsync<AIAgentNotAvailableException>(() => harness.Service.RunAgentAsync(
            limited.Id, Messages, new AIAgentExecutionOptions { UserGroupIds = [Translators] }));
    }

    [Fact]
    public async Task Run_ForAgentThatDoesNotExist_IsNotFoundNotNotAvailable()
    {
        var harness = new Harness(inGroups: [Translators]);

        var failure = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.RunAgentAsync(Guid.NewGuid(), Messages));

        failure.ShouldNotBeOfType<AIAgentNotAvailableException>();
    }

    #endregion

    private static readonly ChatMessage[] Messages = [new ChatMessage(ChatRole.User, "Hello")];

    public enum RunMethod
    {
        RunById,
        RunByAlias,
        StreamById,
        StreamByAlias,
        StreamAGUI,
    }

    /// <summary>
    /// A real <see cref="AIAgentService"/> over the in-memory repository (which applies the same group rule the
    /// SQL query does), a real permission service, and a stubbed agent factory and streaming service.
    /// </summary>
    internal sealed class Harness
    {
        private readonly InMemoryAIAgentRepository _repository = new();

        public Harness(IReadOnlyCollection<Guid>? inGroups, IReadOnlyCollection<string>? permissions = null)
        {
            var accessor = new Mock<IBackOfficeSecurityAccessor>();
            if (inGroups is not null)
            {
                var groups = inGroups.Select(key =>
                {
                    var group = new Mock<IReadOnlyUserGroup>();
                    group.SetupGet(x => x.Key).Returns(key);
                    group.SetupGet(x => x.Permissions).Returns(new HashSet<string>(permissions ?? []));
                    return group.Object;
                }).ToList();
                var user = new Mock<IUser>();
                user.SetupGet(x => x.Groups).Returns(groups);
                var security = new Mock<IBackOfficeSecurity>();
                security.SetupGet(x => x.CurrentUser).Returns(user.Object);
                accessor.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);
            }

            var chatClient = new Mock<IChatClient>();
            chatClient
                .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            chatClient
                .Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
                .Returns(ChatUpdates());
            MsAIAgent respondingAgent = new ChatClientAgent(chatClient.Object);

            var agentFactory = new Mock<IAIAgentFactory>();
            agentFactory
                .Setup(x => x.CreateAgentAsync(
                    It.IsAny<AIAgent>(),
                    It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                    It.IsAny<IEnumerable<AITool>?>(),
                    It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                    It.IsAny<AIApprovalPolicy>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(respondingAgent);

            var streaming = new Mock<IAGUIStreamingService>();
            streaming
                .Setup(x => x.StreamAgentAsync(
                    It.IsAny<MsAIAgent>(),
                    It.IsAny<AGUIRunRequest>(),
                    It.IsAny<IEnumerable<AITool>?>(),
                    It.IsAny<AgentSession?>(),
                    It.IsAny<IReadOnlyDictionary<string, ToolApprovalRequestContent>?>(),
                    It.IsAny<IReadOnlyList<ToolApprovalRequestContent>?>(),
                    It.IsAny<AIConversationPersistenceSync?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Events());

            var messageConverter = new Mock<IAGUIMessageConverter>();
            messageConverter
                .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
                .Returns([new ChatMessage(ChatRole.User, "Hello")]);
            var contextConverter = new Mock<IAGUIContextConverter>();
            contextConverter
                .Setup(x => x.ConvertToRequestContextItems(It.IsAny<IEnumerable<AGUIContextItem>?>()))
                .Returns([]);

            Service = new AIAgentService(
                _repository,
                null!, // IAIEntityVersionService
                agentFactory.Object,
                streaming.Object,
                contextConverter.Object,
                messageConverter.Object,
                new AIToolCollection(() => []),
                null!, // IAIProfileService
                null!, // IAIGuardrailService
                null!, // IAIContextService
                Mock.Of<IEventAggregator>(),
                new AIPermissionService(accessor.Object),
                accessor.Object);
        }

        public AIAgentService Service { get; }

        public AIAgent Add(
            string alias,
            IReadOnlyList<Guid> allowedGroups,
            IReadOnlyList<string>? surfaces = null,
            AIAgentScope? scope = null)
        {
            var agent = new AIAgent
            {
                Id = Guid.NewGuid(),
                Alias = alias,
                Name = alias,
                AgentType = AIAgentType.Standard,
                Config = new AIStandardAgentConfig(),
                IsActive = true,
                AllowedUserGroupIds = allowedGroups,
                SurfaceIds = surfaces ?? [],
                Scope = scope,
            };
            _repository.SaveAsync(agent).GetAwaiter().GetResult();
            return agent;
        }

        public async Task InvokeAsync(RunMethod method, AIAgent agent)
        {
            switch (method)
            {
                case RunMethod.RunById:
                    await Service.RunAgentAsync(agent.Id, Messages);
                    break;
                case RunMethod.RunByAlias:
                    await Service.RunAgentAsync(agent.Alias, Messages);
                    break;
                case RunMethod.StreamById:
                    await foreach (var _ in Service.StreamAgentAsync(agent.Id, Messages))
                    {
                    }

                    break;
                case RunMethod.StreamByAlias:
                    await foreach (var _ in Service.StreamAgentAsync(agent.Alias, Messages))
                    {
                    }

                    break;
                case RunMethod.StreamAGUI:
                    var request = new AGUIRunRequest { ThreadId = "t", RunId = "r", Messages = [] };
                    await foreach (var _ in Service.StreamAgentAGUIAsync(agent.Id, request, frontendTools: null))
                    {
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> ChatUpdates()
        {
            await Task.CompletedTask;
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
        }

        private static async IAsyncEnumerable<IAGUIEvent> Events()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
