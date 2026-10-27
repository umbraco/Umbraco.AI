using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// User group denies win over every grant for front-end (Copilot) tools too, including tools the agent
/// grants through its default scopes.
/// </summary>
public class AIAgentServiceFrontendToolPermissionTests
{
    private static readonly Guid AgentId = Guid.NewGuid();
    private static readonly Guid RestrictedGroup = Guid.NewGuid();
    private static readonly Guid OtherGroup = Guid.NewGuid();

    [Fact]
    public async Task ScopeGrantedTool_DeniedByScopeForTheUsersGroup_IsNotOffered()
    {
        var agent = Agent(allowedScopes: ["entity-publish", "entity-save"], deny: new() { DeniedToolScopeIds = ["entity-publish"] });

        var offered = await OfferedFrontendToolsAsync(agent, [RestrictedGroup], Tool("save_and_publish", "entity-publish"), Tool("save", "entity-save"));

        offered.ShouldBe(["save"]);
    }

    [Fact]
    public async Task ScopeGrantedTool_DeniedByNameForTheUsersGroup_IsNotOffered()
    {
        var agent = Agent(allowedScopes: ["entity-publish", "entity-save"], deny: new() { DeniedToolIds = ["save_and_publish"] });

        var offered = await OfferedFrontendToolsAsync(agent, [RestrictedGroup], Tool("save_and_publish", "entity-publish"), Tool("save", "entity-save"));

        offered.ShouldBe(["save"]);
    }

    [Fact]
    public async Task NameGrantedTool_DeniedByScopeForTheUsersGroup_IsNotOffered()
    {
        var agent = Agent(allowedToolIds: ["set_value"], deny: new() { DeniedToolScopeIds = ["entity-write"] });

        var offered = await OfferedFrontendToolsAsync(agent, [RestrictedGroup], Tool("set_value", "entity-write"));

        offered.ShouldBeEmpty();
    }

    [Fact]
    public async Task DenyForAnotherGroup_DoesNotAffectTheUser()
    {
        var agent = Agent(allowedScopes: ["entity-publish"], deny: new() { DeniedToolScopeIds = ["entity-publish"] });

        var offered = await OfferedFrontendToolsAsync(agent, [OtherGroup], Tool("save_and_publish", "entity-publish"));

        offered.ShouldBe(["save_and_publish"]);
    }

    [Fact]
    public async Task SignedInUsersGroups_AreUsed_WhenNoneArePassed()
    {
        var agent = Agent(allowedScopes: ["entity-publish"], deny: new() { DeniedToolScopeIds = ["entity-publish"] });

        var offered = await OfferedFrontendToolsAsync(agent, userGroupIds: null, Tool("save_and_publish", "entity-publish"));

        offered.ShouldBeEmpty();
    }

    private static AIFrontendTool Tool(string name, string scope)
        => new(new AGUITool { Name = name, Description = name }, scope, IsDestructive: false);

    private static AIAgent Agent(
        AIAgentUserGroupPermissions deny,
        IReadOnlyList<string>? allowedScopes = null,
        IReadOnlyList<string>? allowedToolIds = null)
        => new()
        {
            Id = AgentId,
            Alias = "copilot",
            Name = "Copilot",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig
            {
                AllowedToolIds = allowedToolIds ?? [],
                AllowedToolScopeIds = allowedScopes ?? [],
                UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions> { [RestrictedGroup] = deny },
            },
            IsActive = true,
        };

    /// <summary>
    /// Runs the agent over AG-UI with the given front-end tools and returns the names of the front-end
    /// tools handed to the agent factory (i.e. offered to the model).
    /// </summary>
    private static async Task<IReadOnlyList<string>> OfferedFrontendToolsAsync(
        AIAgent agent,
        IEnumerable<Guid>? userGroupIds,
        params AIFrontendTool[] frontendTools)
    {
        var repository = new Mock<IAIAgentRepository>();
        repository.Setup(x => x.GetByIdAsync(AgentId, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        IEnumerable<AITool>? offered = null;
        var agentFactory = new Mock<IAIAgentFactory>();
        agentFactory
            .Setup(x => x.CreateAgentAsync(
                agent,
                It.IsAny<IEnumerable<AIRequestContextItem>?>(),
                It.IsAny<IEnumerable<AITool>?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<AIApprovalPolicy>(),
                It.IsAny<CancellationToken>()))
            .Callback<AIAgent, IEnumerable<AIRequestContextItem>?, IEnumerable<AITool>?, IReadOnlyDictionary<string, object?>?, AIApprovalPolicy, CancellationToken>(
                (_, _, tools, _, _, _) => offered = tools?.ToList())
            .ReturnsAsync(new Mock<MsAIAgent>().Object);

        var messageConverter = new Mock<IAGUIMessageConverter>();
        messageConverter
            .Setup(x => x.ConvertToChatMessages(It.IsAny<IEnumerable<AGUIMessage>?>()))
            .Returns([new ChatMessage(ChatRole.User, "Publish this page")]);

        var contextConverter = new Mock<IAGUIContextConverter>();
        contextConverter
            .Setup(x => x.ConvertToRequestContextItems(It.IsAny<IEnumerable<AGUIContextItem>?>()))
            .Returns([]);

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
            .Returns(NoEvents());

        // The signed-in user belongs to the restricted group.
        var user = new Mock<IUser>();
        var group = new Mock<IReadOnlyUserGroup>();
        group.SetupGet(x => x.Key).Returns(RestrictedGroup);
        user.SetupGet(x => x.Groups).Returns([group.Object]);
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(user.Object);
        var securityAccessor = new Mock<IBackOfficeSecurityAccessor>();
        securityAccessor.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);

        var service = new AIAgentService(
            repository.Object,
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
            securityAccessor.Object);

        var request = new AGUIRunRequest { RunId = "run-1", ThreadId = "thread-1", Messages = [] };
        var options = new AIAgentExecutionOptions { UserGroupIds = userGroupIds?.ToList() };

        await foreach (var _ in service.StreamAgentAGUIAsync(AgentId, request, frontendTools, options, CancellationToken.None))
        {
        }

        return offered?.Select(t => t.Name).ToList() ?? [];
    }

    private static async IAsyncEnumerable<IAGUIEvent> NoEvents()
    {
        await Task.CompletedTask;
        yield break;
    }
}
