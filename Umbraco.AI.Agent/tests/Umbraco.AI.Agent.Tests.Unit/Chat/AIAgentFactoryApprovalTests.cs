using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.RuntimeContext;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Xunit;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

public class AIAgentFactoryApprovalTests
{
    private static readonly Guid TestProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

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

    private sealed class TestSystemTool : IAITool, IAISystemTool
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

    private sealed class TestSurface : IAIAgentSurface
    {
        public required string Id { get; init; }
        public string Icon => "icon-chat";
        public IReadOnlyList<string> SupportedScopeDimensions => [];
        public bool RestrictsDestructiveBackendTools { get; init; }
    }

    private static AIRequestContextItem SurfaceContextItem(string surfaceId)
        => new() { Description = "surface", Value = $"{{\"surface\":\"{surfaceId}\"}}" };

    [Fact]
    public async Task CreateAgentAsync_DestructiveNonSystemTool_IsWrappedInApprovalRequired()
    {
        IAITool[] tools =
        [
            new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true },
            new TestTool { Id = "get-thing",    Name = "get-thing",    IsDestructive = false },
        ];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["delete-thing", "get-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        var chatOptions = ExtractChatOptions(result);
        chatOptions.ShouldNotBeNull();
        chatOptions!.Tools.ShouldNotBeNull();
        chatOptions.Tools!.Single(t => t.Name == "delete-thing").ShouldBeOfType<ApprovalRequiredAIFunction>();
        chatOptions.Tools!.Single(t => t.Name == "get-thing").ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    }

    [Fact]
    public async Task CreateAgentAsync_WithDestructiveTool_SetsAllowMultipleToolCallsFalse()
    {
        IAITool[] tools = [new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["delete-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        ExtractChatOptions(result)!.AllowMultipleToolCalls.ShouldBe(false);
    }

    [Fact]
    public async Task CreateAgentAsync_WithNoDestructiveTool_LeavesAllowMultipleToolCallsNull()
    {
        IAITool[] tools = [new TestTool { Id = "get-thing", Name = "get-thing", IsDestructive = false }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["get-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        ExtractChatOptions(result)!.AllowMultipleToolCalls.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAgentAsync_DenyAllPolicy_WrapsDestructiveToolInApprovalDenied()
    {
        IAITool[] tools =
        [
            new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true },
            new TestTool { Id = "get-thing",    Name = "get-thing",    IsDestructive = false },
        ];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["delete-thing", "get-thing"]);

        var result = await factory.CreateAgentAsync(agent, approvalPolicy: AIApprovalPolicy.DenyAll);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Tools!.Single(t => t.Name == "delete-thing").ShouldBeOfType<ApprovalDeniedAIFunction>();
        chatOptions.Tools!.Single(t => t.Name == "get-thing").ShouldNotBeOfType<ApprovalDeniedAIFunction>();
        // No ApprovalRequiredAIFunction is produced under DenyAll, so multi-call stays at the default.
        chatOptions.AllowMultipleToolCalls.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAgentAsync_AllowAllPolicy_LeavesDestructiveToolUnwrapped()
    {
        IAITool[] tools = [new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["delete-thing"]);

        var result = await factory.CreateAgentAsync(agent, approvalPolicy: AIApprovalPolicy.AllowAll);

        var tool = ExtractChatOptions(result)!.Tools!.Single(t => t.Name == "delete-thing");
        tool.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
        tool.ShouldNotBeOfType<ApprovalDeniedAIFunction>();
        ExtractChatOptions(result)!.AllowMultipleToolCalls.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAgentAsync_DestructiveSystemTool_IsNotWrappedAndDoesNotSetMultipleToolCallsFalse()
    {
        // System tools are always included even without explicit AllowedToolIds;
        // they must not be wrapped in ApprovalRequiredAIFunction.
        IAITool[] tools = [new TestSystemTool { Id = "sys-delete", Name = "sys-delete", IsDestructive = true }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent([]); // no explicit tool IDs; system tool is auto-included

        var result = await factory.CreateAgentAsync(agent);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Tools!.Single(t => t.Name == "sys-delete").ShouldNotBeOfType<ApprovalRequiredAIFunction>();
        chatOptions.AllowMultipleToolCalls.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAgentAsync_RestrictedSurface_DropsDestructiveBackendTool_KeepsReadTool()
    {
        IAITool[] tools =
        [
            new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true },
            new TestTool { Id = "get-thing",    Name = "get-thing",    IsDestructive = false },
        ];

        var factory = CreateFactory(
            tools,
            surfaces: [new TestSurface { Id = "copilot", RestrictsDestructiveBackendTools = true }],
            contributors: [new SurfaceContextContributor()]);
        var agent = CreateAgent(["delete-thing", "get-thing"]);

        var result = await factory.CreateAgentAsync(agent, contextItems: [SurfaceContextItem("copilot")]);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Tools.ShouldNotBeNull();
        // The destructive backend tool is withheld entirely on a restricted surface...
        chatOptions.Tools!.ShouldNotContain(t => t.Name == "delete-thing");
        // ...while non-destructive backend tools (reads/search) remain available.
        chatOptions.Tools!.ShouldContain(t => t.Name == "get-thing");
    }

    [Fact]
    public async Task CreateAgentAsync_UnrestrictedSurface_KeepsDestructiveBackendTool()
    {
        IAITool[] tools = [new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true }];

        var factory = CreateFactory(
            tools,
            surfaces: [new TestSurface { Id = "workspace", RestrictsDestructiveBackendTools = false }],
            contributors: [new SurfaceContextContributor()]);
        var agent = CreateAgent(["delete-thing"]);

        var result = await factory.CreateAgentAsync(agent, contextItems: [SurfaceContextItem("workspace")]);

        // A surface that doesn't opt in keeps destructive tools (wrapped for approval by default).
        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Tools!.Single(t => t.Name == "delete-thing").ShouldBeOfType<ApprovalRequiredAIFunction>();
    }

    [Fact]
    public async Task CreateAgentAsync_NoSurfaceContext_KeepsDestructiveBackendTool()
    {
        // Belt-and-braces: with no surface context at all, the restriction never engages.
        IAITool[] tools = [new TestTool { Id = "delete-thing", Name = "delete-thing", IsDestructive = true }];

        var factory = CreateFactory(
            tools,
            surfaces: [new TestSurface { Id = "copilot", RestrictsDestructiveBackendTools = true }],
            contributors: [new SurfaceContextContributor()]);
        var agent = CreateAgent(["delete-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Tools!.Single(t => t.Name == "delete-thing").ShouldBeOfType<ApprovalRequiredAIFunction>();
    }

    [Fact]
    public async Task CreateAgentAsync_CopiesTemperatureAndMaxTokensFromProfileSettings()
    {
        IAITool[] tools = [new TestTool { Id = "get-thing", Name = "get-thing", IsDestructive = false }];

        var factory = CreateFactory(
            tools,
            new AIChatProfileSettings { Temperature = 0.5f, MaxTokens = 64000 });
        var agent = CreateAgent(["get-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Temperature.ShouldBe(0.5f);
        chatOptions.MaxOutputTokens.ShouldBe(64000);
    }

    [Fact]
    public async Task CreateAgentAsync_WithNoChatSettings_LeavesInferenceOptionsNull()
    {
        IAITool[] tools = [new TestTool { Id = "get-thing", Name = "get-thing", IsDestructive = false }];

        var factory = CreateFactory(tools);
        var agent = CreateAgent(["get-thing"]);

        var result = await factory.CreateAgentAsync(agent);

        var chatOptions = ExtractChatOptions(result);
        chatOptions!.Temperature.ShouldBeNull();
        chatOptions.MaxOutputTokens.ShouldBeNull();
    }

    private static AIAgentFactory CreateFactory(
        IEnumerable<IAITool> tools,
        IAIProfileSettings? profileSettings = null,
        IEnumerable<IAIAgentSurface>? surfaces = null,
        IEnumerable<IAIRuntimeContextContributor>? contributors = null)
    {
        var toolCollection = new AIToolCollection(() => tools);
        var scopeCollection = new AIToolScopeCollection(() => []);
        var workflowCollection = new AIAgentWorkflowCollection(() => []);
        var contributorCollection = new AIRuntimeContextContributorCollection(() => contributors ?? []);
        var surfaceCollection = new AIAgentSurfaceCollection(() => surfaces ?? []);

        var functionFactoryMock = new Mock<IAIFunctionFactory>();
        functionFactoryMock
            .Setup(f => f.Create(It.IsAny<IEnumerable<IAITool>>()))
            .Returns<IEnumerable<IAITool>>(ts =>
                ts.Select(t => Microsoft.Extensions.AI.AIFunctionFactory.Create(() => "ok", name: t.Id)).ToList());

        var chatClientMock = new Mock<IChatClient>();
        chatClientMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var dummyProfile = new AIProfile
        {
            Id = TestProfileId,
            Alias = "test",
            Name = "Test",
            ConnectionId = Guid.Empty,
            Settings = profileSettings,
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
            surfaceCollection);
    }

    private static UmbracoAIAgent CreateAgent(IReadOnlyList<string> allowedToolIds)
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
            },
            IsActive = true,
        };

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
