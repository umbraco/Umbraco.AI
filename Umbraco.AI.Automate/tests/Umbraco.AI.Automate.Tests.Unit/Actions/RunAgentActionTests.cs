using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Services;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using CoreConstants = Umbraco.AI.Core.Constants;

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
    public async Task ExecuteAsync_WithValidAgent_ReturnsSuccess()
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
    }

    [Fact]
    public async Task ExecuteAsync_WithPlainTextResponse_ExposesRawResponseProperty()
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
        var output = result.OutputData.ShouldBeOfType<Dictionary<string, object?>>();
        output[RunAgentAction.RawResponseKey]?.ToString().ShouldBe("Hello from agent!");
    }

    [Fact]
    public async Task ExecuteAsync_PopulatesAuditMetadataKeysOnExecutionOptions()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
        };

        var responseMessage = new ChatMessage(ChatRole.Assistant, "ok");
        var agentResponse = new AgentResponse(responseMessage);
        var automationRunId = Guid.NewGuid();

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        AIAgentExecutionOptions? capturedOptions = null;
        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<ChatMessage>, AIAgentExecutionOptions?, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts)
            .ReturnsAsync(agentResponse);

        var action = CreateAction();
        var context = CreateContext(
            new RunAgentSettings { AgentId = TestAgentId, Message = "Hi" },
            runId: automationRunId);

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        capturedOptions.ShouldNotBeNull();
        capturedOptions!.AdditionalProperties.ShouldNotBeNull();

        var props = capturedOptions.AdditionalProperties!;
        props.Keys.ShouldContain(Constants.ContextKeys.RunId);
        props.Keys.ShouldContain(Constants.ContextKeys.ThreadId);
        props.Keys.ShouldContain(CoreConstants.ContextKeys.LogKeys);

        Guid.TryParse(props[Constants.ContextKeys.RunId]?.ToString(), out _)
            .ShouldBeTrue();
        props[Constants.ContextKeys.ThreadId]
            .ShouldBe(automationRunId.ToString());

        var logKeys = props[CoreConstants.ContextKeys.LogKeys].ShouldBeOfType<string[]>();
        logKeys.ShouldBe(
            new[] { Constants.ContextKeys.RunId, Constants.ContextKeys.ThreadId },
            ignoreOrder: true);
    }

    [Fact]
    public async Task ExecuteAsync_WithStructuredJsonResponse_ParsesOutput()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
        };

        var jsonResponse = """{"summary": "A test summary", "score": 42}""";
        var responseMessage = new ChatMessage(ChatRole.Assistant, jsonResponse);
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
            Message = "Summarize",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        var output = result.OutputData.ShouldBeOfType<Dictionary<string, object?>>();
        output["summary"]?.ToString().ShouldBe("A test summary");

        // The raw response is always exposed alongside the parsed structured properties.
        output.ShouldContainKey(RunAgentAction.RawResponseKey);
        output[RunAgentAction.RawResponseKey]?.ToString().ShouldBe(jsonResponse);
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

    private static ActionContext CreateContext(RunAgentSettings settings, Guid? runId = null)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = runId ?? Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.RunAgent,
            Settings = settings,
        };
}
