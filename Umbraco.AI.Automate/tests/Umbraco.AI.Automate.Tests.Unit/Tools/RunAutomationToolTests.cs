using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Automate.Tools;
using Umbraco.AI.Core.Tools;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Tools;

public class RunAutomationToolTests
{
    private readonly Mock<IAutomationService> _automationServiceMock = new();
    private readonly Mock<IAutomationExecutor> _automationExecutorMock = new();
    private readonly Mock<IWorkspaceService> _workspaceServiceMock = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessorMock = new();

    private static readonly Guid TestAutomationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestWorkspaceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestRunId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TestUserGroupKey = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task ExecuteAsync_WithValidAutomation_ReturnsSuccess()
    {
        // Arrange
        SetupAuthenticatedUser();
        SetupWorkspaceAccess(TestWorkspaceId);

        var automation = CreateAutomation(TestAutomationId, "Test Automation",
            triggerAlias: UmbracoAIAutomateConstants.TriggerTypes.AgentTrigger);

        _automationServiceMock
            .Setup(s => s.GetAutomationAsync(TestAutomationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(automation);

        _automationExecutorMock
            .Setup(e => e.ExecuteAsync(
                automation,
                "ai-agent",
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>?>(),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(TestRunId);

        var tool = CreateTool();

        // Act
        var result = await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId, "Hello"));

        // Assert
        var typedResult = result.ShouldBeOfType<RunAutomationResult>();
        typedResult.Success.ShouldBeTrue();
        typedResult.RunId.ShouldBe(TestRunId);
        typedResult.AutomationName.ShouldBe("Test Automation");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAutomationId_ReturnsError()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var result = await InvokeToolAsync(tool, new RunAutomationArgs(Guid.Empty));

        // Assert
        var typedResult = result.ShouldBeOfType<RunAutomationResult>();
        typedResult.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUser_ReturnsError()
    {
        // Arrange — no user set up
        var tool = CreateTool();

        // Act
        var result = await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId));

        // Assert
        var typedResult = result.ShouldBeOfType<RunAutomationResult>();
        typedResult.Success.ShouldBeFalse();
        typedResult.Message.ShouldContain("authenticated");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoWorkspaceAccess_ReturnsError()
    {
        // Arrange
        SetupAuthenticatedUser();
        SetupWorkspaceAccess(); // No accessible workspaces

        var automation = CreateAutomation(TestAutomationId, "Test Automation",
            triggerAlias: "umbracoAutomate.manual");

        _automationServiceMock
            .Setup(s => s.GetAutomationAsync(TestAutomationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(automation);

        var tool = CreateTool();

        // Act
        var result = await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId));

        // Assert
        var typedResult = result.ShouldBeOfType<RunAutomationResult>();
        typedResult.Success.ShouldBeFalse();
        typedResult.Message.ShouldContain("workspace");
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongTriggerType_ReturnsError()
    {
        // Arrange
        SetupAuthenticatedUser();
        SetupWorkspaceAccess(TestWorkspaceId);

        var automation = CreateAutomation(TestAutomationId, "Event Automation",
            triggerAlias: "umbracoAutomate.contentPublished");

        _automationServiceMock
            .Setup(s => s.GetAutomationAsync(TestAutomationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(automation);

        var tool = CreateTool();

        // Act
        var result = await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId));

        // Assert
        var typedResult = result.ShouldBeOfType<RunAutomationResult>();
        typedResult.Success.ShouldBeFalse();
        typedResult.Message.ShouldContain("Manual or AI Agent trigger");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesNestingDepthInTriggerOutputData()
    {
        // Arrange
        SetupAuthenticatedUser();
        SetupWorkspaceAccess(TestWorkspaceId);

        var automation = CreateAutomation(TestAutomationId, "Test Automation",
            triggerAlias: "umbracoAutomate.manual");

        _automationServiceMock
            .Setup(s => s.GetAutomationAsync(TestAutomationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(automation);

        Dictionary<string, object?>? capturedTriggerData = null;
        _automationExecutorMock
            .Setup(e => e.ExecuteAsync(
                automation,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>?>(),
                It.IsAny<CancellationToken>(),
                false))
            .Callback<Automation, string, string?, Dictionary<string, object?>?, CancellationToken, bool>(
                (_, _, _, triggerData, _, _) => capturedTriggerData = triggerData)
            .ReturnsAsync(TestRunId);

        var tool = CreateTool();

        // Act
        await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId));

        // Assert
        capturedTriggerData.ShouldNotBeNull();
        capturedTriggerData.ShouldContainKey(RunAgentAction.AgentNestingDepthKey);
        capturedTriggerData[RunAgentAction.AgentNestingDepthKey].ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithAgentTriggerAndMessage_IncludesMessageInTriggerData()
    {
        // Arrange
        SetupAuthenticatedUser();
        SetupWorkspaceAccess(TestWorkspaceId);

        var automation = CreateAutomation(TestAutomationId, "Test Automation",
            triggerAlias: UmbracoAIAutomateConstants.TriggerTypes.AgentTrigger);

        _automationServiceMock
            .Setup(s => s.GetAutomationAsync(TestAutomationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(automation);

        Dictionary<string, object?>? capturedTriggerData = null;
        _automationExecutorMock
            .Setup(e => e.ExecuteAsync(
                automation,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>?>(),
                It.IsAny<CancellationToken>(),
                false))
            .Callback<Automation, string, string?, Dictionary<string, object?>?, CancellationToken, bool>(
                (_, _, _, triggerData, _, _) => capturedTriggerData = triggerData)
            .ReturnsAsync(TestRunId);

        var tool = CreateTool();

        // Act
        await InvokeToolAsync(tool, new RunAutomationArgs(TestAutomationId, "Send welcome email"));

        // Assert
        capturedTriggerData.ShouldNotBeNull();
        capturedTriggerData.ShouldContainKey("message");
        capturedTriggerData["message"].ShouldBe("Send welcome email");
    }

    private RunAutomationTool CreateTool()
        => new(
            _automationServiceMock.Object,
            _automationExecutorMock.Object,
            _workspaceServiceMock.Object,
            _securityAccessorMock.Object);

    private static async Task<object> InvokeToolAsync(IAITool tool, RunAutomationArgs args)
        => await tool.ExecuteAsync(args);

    private void SetupAuthenticatedUser()
    {
        var userGroup = new Mock<IReadOnlyUserGroup>();
        userGroup.Setup(g => g.Key).Returns(TestUserGroupKey);

        var user = new Mock<IUser>();
        user.Setup(u => u.Key).Returns(Guid.NewGuid());
        user.Setup(u => u.Groups).Returns(new[] { userGroup.Object });

        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(s => s.CurrentUser).Returns(user.Object);

        _securityAccessorMock.Setup(a => a.BackOfficeSecurity).Returns(security.Object);
    }

    private void SetupWorkspaceAccess(params Guid[] workspaceIds)
    {
        var ids = new HashSet<Guid>(workspaceIds) as IReadOnlySet<Guid>;
        _workspaceServiceMock
            .Setup(w => w.GetAccessibleWorkspaceIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
    }

    private static Automation CreateAutomation(Guid id, string name, string triggerAlias,
        AutomationStatus status = AutomationStatus.Published, bool isEnabled = true)
    {
        // Use reflection to set the internal Id property
        var automation = new Automation
        {
            Alias = name.ToLowerInvariant().Replace(' ', '-'),
            Name = name,
            WorkspaceId = TestWorkspaceId,
            Status = status,
            IsEnabled = isEnabled,
            Trigger = new TriggerConfiguration
            {
                TriggerAlias = triggerAlias,
            },
        };

        // Set Id via reflection (internal setter)
        typeof(Automation).GetProperty(nameof(Automation.Id))!.SetValue(automation, id);

        return automation;
    }
}
