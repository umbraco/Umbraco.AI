using System.Reflection;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Covers the server-side tool permission boundary. The factory builds the tool list the model
/// actually receives, so if it ignores per-user-group permissions then a user in a group that
/// denies a tool still gets that tool — regardless of what the frontend was allowed to send.
/// </summary>
public class AIAgentFactoryToolPermissionTests
{
    private static readonly Guid TestProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid EditorsGroupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class TestTool : IAITool
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string Description => string.Empty;
        public string ScopeId => "test-scope";
        public bool IsDestructive { get; init; }
        public IReadOnlyList<string> Tags => [];
        public Type? ArgsType => null;
        public Task<object> ExecuteAsync(object? args, CancellationToken cancellationToken = default)
            => Task.FromResult<object>("result");
    }

    [Fact]
    public async Task CreateAgentAsync_WhenCallerSuppliesAllowedToolIds_BuildsToolListFromThem()
    {
        // Arrange
        // The agent's own config allows both tools, but the caller (AIAgentService) resolved a
        // narrower set after applying user group permissions. The narrower set must win.
        IAITool[] tools =
        [
            new TestTool { Id = "get-thing", Name = "get-thing" },
            new TestTool { Id = "delete-thing", Name = "delete-thing" },
        ];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["get-thing", "delete-thing"]);

        var additionalProperties = new Dictionary<string, object?>
        {
            [AgentConstants.ContextKeys.AllowedToolIds] = new List<string> { "get-thing" },
        };

        // Act
        var result = await factory.CreateAgentAsync(agent, additionalProperties: additionalProperties);

        // Assert
        var toolNames = ExtractToolNames(result);
        toolNames.ShouldContain("get-thing");
        toolNames.ShouldNotContain("delete-thing");
    }

    [Fact]
    public async Task CreateAgentAsync_WhenCallerSuppliesEmptyAllowedToolIds_BuildsNoTools()
    {
        // Arrange
        // An empty supplied set is a real decision ("this user may use nothing"), not missing data.
        // It must not be mistaken for "not supplied" and fall back to the agent's defaults.
        IAITool[] tools = [new TestTool { Id = "delete-thing", Name = "delete-thing" }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["delete-thing"]);

        var additionalProperties = new Dictionary<string, object?>
        {
            [AgentConstants.ContextKeys.AllowedToolIds] = new List<string>(),
        };

        // Act
        var result = await factory.CreateAgentAsync(agent, additionalProperties: additionalProperties);

        // Assert
        ExtractToolNames(result).ShouldNotContain("delete-thing");
    }

    [Fact]
    public async Task CreateAgentAsync_WithNoSuppliedToolIds_AppliesCurrentUsersGroupDenies()
    {
        // Arrange
        // Direct factory callers supply nothing. The factory must still resolve the acting user's
        // groups rather than ignoring group permissions altogether.
        IAITool[] tools =
        [
            new TestTool { Id = "get-thing", Name = "get-thing" },
            new TestTool { Id = "delete-thing", Name = "delete-thing" },
        ];

        var factory = CreateFactory(tools, securityAccessor: CreateSecurityAccessor(EditorsGroupId));
        var agent = CreateAgent(
            ["get-thing", "delete-thing"],
            userGroupPermissions: new Dictionary<Guid, AIAgentUserGroupPermissions>
            {
                [EditorsGroupId] = new() { DeniedToolIds = ["delete-thing"] },
            });

        // Act
        var result = await factory.CreateAgentAsync(agent);

        // Assert
        var toolNames = ExtractToolNames(result);
        toolNames.ShouldContain("get-thing");
        toolNames.ShouldNotContain("delete-thing");
    }

    [Fact]
    public async Task CreateAgentAsync_WithNoUserContext_FallsBackToAgentDefaults()
    {
        // Arrange
        // Background jobs and programmatic runs have no current user. They must keep working with
        // the agent's own defaults rather than being stripped of every tool.
        IAITool[] tools =
        [
            new TestTool { Id = "get-thing", Name = "get-thing" },
            new TestTool { Id = "delete-thing", Name = "delete-thing" },
        ];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(
            ["get-thing", "delete-thing"],
            userGroupPermissions: new Dictionary<Guid, AIAgentUserGroupPermissions>
            {
                [EditorsGroupId] = new() { DeniedToolIds = ["delete-thing"] },
            });

        // Act
        var result = await factory.CreateAgentAsync(agent);

        // Assert
        var toolNames = ExtractToolNames(result);
        toolNames.ShouldContain("get-thing");
        toolNames.ShouldContain("delete-thing");
    }

    private static Mock<IBackOfficeSecurityAccessor> CreateSecurityAccessor(params Guid[] groupIds)
    {
        var groups = groupIds
            .Select(id =>
            {
                var group = new Mock<IReadOnlyUserGroup>();
                group.Setup(g => g.Key).Returns(id);
                return group.Object;
            })
            .ToList();

        var user = new Mock<IUser>();
        user.Setup(u => u.Groups).Returns(groups);

        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(s => s.CurrentUser).Returns(user.Object);

        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(a => a.BackOfficeSecurity).Returns(security.Object);
        return accessor;
    }

    private static AIAgentFactory CreateFactory(
        IEnumerable<IAITool> tools,
        Mock<IBackOfficeSecurityAccessor>? securityAccessor = null)
    {
        var toolCollection = new AIToolCollection(() => tools);
        var scopeCollection = new AIToolScopeCollection(() => []);
        var workflowCollection = new AIAgentWorkflowCollection(() => []);
        var contributorCollection = new AIRuntimeContextContributorCollection(() => []);

        var functionFactoryMock = new Mock<IAIFunctionFactory>();
        functionFactoryMock
            .Setup(f => f.Create(It.IsAny<IEnumerable<IAITool>>()))
            .Returns<IEnumerable<IAITool>>(ts =>
                ts.Select(t => Microsoft.Extensions.AI.AIFunctionFactory.Create(() => "ok", name: t.Id)).ToList());

        var chatClientMock = new Mock<IChatClient>();

        var dummyProfile = new AIProfile
        {
            Id = TestProfileId,
            Alias = "test",
            Name = "Test",
            ConnectionId = Guid.Empty,
        };

        var profileServiceMock = new Mock<IAIProfileService>();
        profileServiceMock
            .Setup(x => x.GetProfileAsync(TestProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyProfile);

        var chatClientFactoryMock = new Mock<IAIChatClientFactory>();
        chatClientFactoryMock
            .Setup(x => x.CreateClientAsync(dummyProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatClientMock.Object);

        return new AIAgentFactory(
            new Mock<IAIRuntimeContextScopeProvider>().Object,
            contributorCollection,
            profileServiceMock.Object,
            chatClientFactoryMock.Object,
            toolCollection,
            scopeCollection,
            functionFactoryMock.Object,
            workflowCollection,
            securityAccessor?.Object);
    }

    private static UmbracoAIAgent CreateAgent(
        IReadOnlyList<string> allowedToolIds,
        IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions>? userGroupPermissions = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = TestProfileId,
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig
            {
                AllowedToolIds = allowedToolIds,
                AllowedToolScopeIds = [],
                UserGroupPermissions = userGroupPermissions
                    ?? new Dictionary<Guid, AIAgentUserGroupPermissions>(),
            },
            IsActive = true,
        };

    private static IReadOnlyList<string> ExtractToolNames(MsAIAgent agent)
        => ExtractChatOptions(agent)?.Tools?.Select(t => t.Name).ToList() ?? [];

    private static ChatOptions? ExtractChatOptions(MsAIAgent agent)
    {
        // agent is ScopedAIAgent : DelegatingAIAgent; InnerAgent is the ChatClientAgent
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var innerAgent = agent.GetType().BaseType?
            .GetProperty("InnerAgent", flags)?
            .GetValue(agent) as MsAIAgent;
        if (innerAgent == null) return null;
        return (ChatOptions?)innerAgent.GetType()
            .GetProperty("ChatOptions", flags)?
            .GetValue(innerAgent);
    }
}
