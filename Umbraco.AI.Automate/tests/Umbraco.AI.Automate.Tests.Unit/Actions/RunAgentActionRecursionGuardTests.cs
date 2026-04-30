using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class RunAgentActionRecursionGuardTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILogger<RunAgentAction>> _loggerMock = new();
    private readonly ActionInfrastructure _infrastructure;

    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public RunAgentActionRecursionGuardTests()
    {
        _infrastructure = new ActionInfrastructure(new Mock<IEditableModelResolver>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNestingDepthExceedsMax_ReturnsValidationError()
    {
        // Arrange
        var action = CreateAction();
        var context = CreateContextWithNestingDepth(
            new RunAgentSettings { AgentId = TestAgentId, Message = "Hello" },
            nestingDepth: RunAgentAction.MaxAgentNestingDepth);

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain("nesting depth");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNestingDepthBelowMax_Proceeds()
    {
        // Arrange
        var agent = new AIAgent { Alias = "test-agent", Name = "Test Agent" };

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Agents.AI.AgentResponse(
                new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, "Hello!")));

        var action = CreateAction();
        var context = CreateContextWithNestingDepth(
            new RunAgentSettings { AgentId = TestAgentId, Message = "Hello" },
            nestingDepth: 1);

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutNestingDepth_Proceeds()
    {
        // Arrange
        var agent = new AIAgent { Alias = "test-agent", Name = "Test Agent" };

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Agents.AI.AgentResponse(
                new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, "Hello!")));

        var action = CreateAction();
        var context = new ActionContext
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.RunAgent,
            Settings = new RunAgentSettings { AgentId = TestAgentId, Message = "Hello" },
        };

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
    }

    private RunAgentAction CreateAction()
        => new(_infrastructure, _agentServiceMock.Object, _userServiceMock.Object, _loggerMock.Object);

    private static ActionContext CreateContextWithNestingDepth(RunAgentSettings settings, int nestingDepth)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.RunAgent,
            Settings = settings,
            BindingData = new Dictionary<string, object?>
            {
                ["trigger"] = new Dictionary<string, object?>
                {
                    [RunAgentAction.AgentNestingDepthKey] = nestingDepth,
                },
            },
        };
}
