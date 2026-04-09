using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Services;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class RunAgentActionTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILogger<RunAgentAction>> _loggerMock = new();
    private readonly ActionInfrastructure _infrastructure;

    // Use a fixed ID for tests since AIAgent.Id has an internal setter
    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public RunAgentActionTests()
    {
        _infrastructure = new ActionInfrastructure(new Mock<IEditableModelResolver>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAgent_ReturnsSuccessWithOutput()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
        };

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Hello from agent!");
        var agentResponse = new AgentResponse(responseMessage);

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agentResponse);

        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = TestAgentId,
            Message = "Hello",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        result.OutputData.ShouldNotBeNull();

        var output = result.OutputData.ShouldBeOfType<RunAgentOutput>();
        output.AgentAlias.ShouldBe("test-agent");
        output.IsSuccess.ShouldBeTrue();
        output.Response.ShouldBe("Hello from agent!");
        output.DurationMs.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAgentId_ReturnsValidationError()
    {
        // Arrange
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = Guid.Empty,
            Message = "Hello",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_WithAgentNotFound_ReturnsValidationError()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent?)null);

        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = agentId,
            Message = "Hello",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ReturnsCancelledError()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
        };

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = TestAgentId,
            Message = "Hello",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Cancelled);
    }

    private RunAgentAction CreateAction()
        => new(_infrastructure, _agentServiceMock.Object, _userServiceMock.Object, _loggerMock.Object);

    private static ActionContext CreateContext(RunAgentSettings settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.RunAgent,
            Settings = settings,
        };
}
