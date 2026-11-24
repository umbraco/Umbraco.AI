using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Automate.Security;
using Umbraco.AI.Core.Media;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using Xunit;

using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

/// <summary>
/// Whether the workspace's service account may use the agent, at run time (GP-16) and at publish (GP-15).
/// </summary>
public class RunAgentActionAccessTests
{
    private static readonly Guid AgentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid WorkspaceId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ServiceAccountKey = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid AutomationGroup = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IAIAgentUnrestrictedReader> _readerMock = new();
    private readonly Mock<IWorkspaceServiceAccountResolver> _resolverMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly AIAgent _agent = new() { Alias = "limited-agent", Name = "Limited agent" };

    public RunAgentActionAccessTests()
    {
        _readerMock
            .Setup(r => r.GetAgentUnrestrictedAsync(AgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, "done")));
    }

    // --- GP-16: run time ---

    [Fact]
    public async Task Run_WhenServiceAccountCanUseTheAgent_Succeeds()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);

        var result = await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        result.Status.ShouldBe(ActionResultStatus.Success);
    }

    [Fact]
    public async Task Run_WhenServiceAccountCanUseTheAgent_PassesItsGroupsToTheRun()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);
        var captured = CaptureRunOptions();

        await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        captured().UserGroupIds.ShouldBe([AutomationGroup]);
    }

    [Fact]
    public async Task Run_WhenServiceAccountCannotUseTheAgent_FailsWithAuthentication()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(Guid.NewGuid());

        var result = await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task Run_WhenServiceAccountCannotUseTheAgent_GivesTheReasonFromTheSpec()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(Guid.NewGuid());

        var result = await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        result.Exception!.Message.ShouldBe(
            "The workspace's service account can't use agent 'limited-agent'. Add one of its user groups to the agent, or pick another agent.");
    }

    [Fact]
    public async Task Run_WhenServiceAccountCannotUseTheAgent_DoesNotRunIt()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(Guid.NewGuid());

        await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        VerifyAgentNeverRun();
    }

    [Fact]
    public async Task Run_WithNoServiceAccountAndALimitedAgent_Fails()
    {
        GivenAgentUsableBy(Guid.NewGuid());

        var result = await CreateAction().ExecuteAsync(CreateRunContext(withServiceAccount: false), CancellationToken.None);

        result.Status.ShouldBe(ActionResultStatus.Failed);
    }

    [Fact]
    public async Task Run_WithNoServiceAccountAndALimitedAgent_FailsWithAuthentication()
    {
        GivenAgentUsableBy(Guid.NewGuid());

        var result = await CreateAction().ExecuteAsync(CreateRunContext(withServiceAccount: false), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task Run_WithNoServiceAccountAndALimitedAgent_DoesNotRunIt()
    {
        GivenAgentUsableBy(Guid.NewGuid());

        await CreateAction().ExecuteAsync(CreateRunContext(withServiceAccount: false), CancellationToken.None);

        VerifyAgentNeverRun();
    }

    [Fact]
    public async Task Run_WithNoServiceAccount_ChecksTheAgentAgainstNoGroupsNotTheCurrentUser()
    {
        GivenAgentUsableBy(Guid.NewGuid());

        await CreateAction().ExecuteAsync(CreateRunContext(withServiceAccount: false), CancellationToken.None);

        _agentServiceMock.Verify(
            s => s.GetAgentAsync(AgentId, It.Is<IReadOnlyCollection<Guid>>(g => g.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithNoServiceAccountAndAnUnlimitedAgent_RunsWithNoGroupsNotTheCurrentUsers()
    {
        _agentServiceMock
            .Setup(s => s.GetAgentAsync(AgentId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_agent);
        var captured = CaptureRunOptions();

        await CreateAction().ExecuteAsync(CreateRunContext(withServiceAccount: false), CancellationToken.None);

        captured().UserGroupIds.ShouldNotBeNull();
        captured().UserGroupIds!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_WhenTheAgentServiceRefusesTheRun_FailsWithAuthentication()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);
        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIAgentNotAvailableException(AgentId));

        var result = await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact]
    public async Task Run_WhenTheAgentServiceRefusesTheRun_GivesTheReasonFromTheSpec()
    {
        GivenServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);
        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIAgentNotAvailableException(AgentId));

        var result = await CreateAction().ExecuteAsync(CreateRunContext(), CancellationToken.None);

        result.Exception!.Message.ShouldBe(
            "The workspace's service account can't use agent 'limited-agent'. Add one of its user groups to the agent, or pick another agent.");
    }

    // --- GP-15: publish ---

    [Fact]
    public async Task Publish_WhenServiceAccountCanUseTheAgent_HasNoErrors()
    {
        GivenWorkspaceServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);

        var errors = await CreateAction().ValidateSettingsForPublishAsync(Settings(), Automation(), CancellationToken.None);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_WhenServiceAccountCannotUseTheAgent_ReportsOneError()
    {
        GivenWorkspaceServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(Guid.NewGuid());

        var errors = await CreateAction().ValidateSettingsForPublishAsync(Settings(), Automation(), CancellationToken.None);

        errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Publish_WhenServiceAccountCannotUseTheAgent_NamesTheAgent()
    {
        GivenWorkspaceServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(Guid.NewGuid());

        var errors = await CreateAction().ValidateSettingsForPublishAsync(Settings(), Automation(), CancellationToken.None);

        errors[0].ShouldContain("limited-agent");
    }

    [Fact]
    public async Task Publish_WhenTheWorkspaceHasNoServiceAccountAndTheAgentIsLimited_ReportsAnError()
    {
        _resolverMock
            .Setup(r => r.GetServiceAccountAsync(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IUser?)null);
        GivenAgentUsableBy(Guid.NewGuid());

        var errors = await CreateAction().ValidateSettingsForPublishAsync(Settings(), Automation(), CancellationToken.None);

        errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Publish_ChecksTheServiceAccountOfTheAutomationsWorkspace()
    {
        GivenWorkspaceServiceAccountWithGroups(AutomationGroup);
        GivenAgentUsableBy(AutomationGroup);

        await CreateAction().ValidateSettingsForPublishAsync(Settings(), Automation(), CancellationToken.None);

        _agentServiceMock.Verify(
            s => s.GetAgentAsync(AgentId, It.Is<IReadOnlyCollection<Guid>>(g => g.SequenceEqual(new[] { AutomationGroup })), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Publish_WithNoAgentChosen_LeavesThatToTheRunTimeCheck()
    {
        var errors = await CreateAction().ValidateSettingsForPublishAsync(
            new RunAgentSettings { AgentId = Guid.Empty, Message = "Hi" },
            Automation(),
            CancellationToken.None);

        errors.ShouldBeEmpty();
    }

    // --- helpers ---

    private static RunAgentSettings Settings() => new() { AgentId = AgentId, Message = "Hi" };

    private static Automation Automation() => new() { Alias = "a", Name = "A", WorkspaceId = WorkspaceId };

    private static IUser UserWithGroups(params Guid[] groupKeys)
    {
        var groups = groupKeys.Select(k =>
        {
            var group = new Mock<IReadOnlyUserGroup>();
            group.SetupGet(g => g.Key).Returns(k);
            return group.Object;
        }).ToList();
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Groups).Returns(groups);
        return user.Object;
    }

    private void GivenServiceAccountWithGroups(params Guid[] groupKeys)
        => _userServiceMock
            .Setup(s => s.GetAsync(ServiceAccountKey))
            .ReturnsAsync(UserWithGroups(groupKeys));

    private void GivenWorkspaceServiceAccountWithGroups(params Guid[] groupKeys)
        => _resolverMock
            .Setup(r => r.GetServiceAccountAsync(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWithGroups(groupKeys));

    // The agent service decides the limit: it hands the agent back only to a caller in this group.
    private void GivenAgentUsableBy(Guid allowedGroup)
        => _agentServiceMock
            .Setup(s => s.GetAgentAsync(AgentId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> groups, CancellationToken _) =>
                groups.Contains(allowedGroup) ? _agent : null);

    private Func<AIAgentExecutionOptions> CaptureRunOptions()
    {
        AIAgentExecutionOptions? captured = null;
        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<ChatMessage>, AIAgentExecutionOptions?, CancellationToken>((_, _, o, _) => captured = o)
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, "done")));
        return () => captured!;
    }

    private void VerifyAgentNeverRun()
        => _agentServiceMock.Verify(
            s => s.RunAgentAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

    private RunAgentAction CreateAction()
        => new(
            new ActionInfrastructure(new Mock<IEditableModelResolver>().Object),
            _agentServiceMock.Object,
            _userServiceMock.Object,
            Mock.Of<IMediaService>(),
            Mock.Of<IAIUmbracoMediaResolver>(),
            Mock.Of<IAutomationActionAuthorizer>(),
            new AgentAccessChecker(_agentServiceMock.Object, _readerMock.Object, _resolverMock.Object),
            Mock.Of<ILogger<RunAgentAction>>());

    private static ActionContext CreateRunContext(bool withServiceAccount = true)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.RunAgent,
            Settings = Settings(),
            ExecutionContext = new AutomationExecutionContext
            {
                ServiceAccountKey = withServiceAccount ? ServiceAccountKey : Guid.Empty,
                WorkspaceId = WorkspaceId,
                WorkspaceName = "W",
                AutomationId = Guid.NewGuid(),
                AutomationName = "A",
                RunId = Guid.NewGuid(),
                InitiatorType = "test",
                AllowedConnections = [],
            },
        };
}
