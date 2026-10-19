using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Media;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class RunAgentActionTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IMediaService> _mediaServiceMock = new();
    private readonly Mock<IAIUmbracoMediaResolver> _mediaResolverMock = new();
    private readonly Mock<IAutomationActionAuthorizer> _authorizerMock = new();
    private readonly Mock<ILogger<RunAgentAction>> _loggerMock = new();
    private readonly ActionInfrastructure _infrastructure;

    // Use a fixed ID for tests since AIAgent.Id has an internal setter
    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public RunAgentActionTests()
    {
        _infrastructure = new ActionInfrastructure(new Mock<IEditableModelResolver>().Object);

        _authorizerMock.Setup(a => a.AuthorizeMediaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutomationAuthorizationResult.Success);
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

    [Theory]
    [InlineData(null, AIApprovalPolicy.DenyAll)]
    [InlineData("ReadOnly", AIApprovalPolicy.DenyAll)]
    [InlineData("NoApprovalRequired", AIApprovalPolicy.DenyApprovalRequired)]
    [InlineData("noapprovalrequired", AIApprovalPolicy.DenyApprovalRequired)]
    [InlineData("SomethingElse", AIApprovalPolicy.DenyAll)]
    [InlineData("99", AIApprovalPolicy.DenyAll)]
    public async Task ExecuteAsync_MapsToolPermissionsToApprovalPolicy(string? toolPermissions, AIApprovalPolicy expected)
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

        AIAgentExecutionOptions? capturedOptions = null;
        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<ChatMessage>, AIAgentExecutionOptions?, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var settings = new RunAgentSettings { AgentId = TestAgentId, Message = "Hi" };
        if (toolPermissions is not null)
        {
            settings.ToolPermissions = toolPermissions;
        }

        var action = CreateAction();
        var context = CreateContext(settings);

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        capturedOptions.ShouldNotBeNull();
        capturedOptions!.ApprovalPolicy.ShouldBe(expected);
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

    [Theory]
    [InlineData(AIProviderErrorCategory.RateLimited, StepRunErrorCategory.RateLimiting)]
    [InlineData(AIProviderErrorCategory.Transient, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(AIProviderErrorCategory.Authentication, StepRunErrorCategory.Authentication)]
    [InlineData(AIProviderErrorCategory.NotFound, StepRunErrorCategory.ConfigurationError)]
    public async Task ExecuteAsync_WhenTheProviderFails_ReportsTheMatchingErrorCategory(
        AIProviderErrorCategory providerCategory,
        StepRunErrorCategory expected)
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

        var providerError = new AIProviderException(
            new AIProviderErrorInfo(providerCategory, "user-safe message", ProviderCode: null, "raw message"),
            new Exception("sdk failure"));

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(providerError);

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
        result.ErrorCategory.ShouldBe(expected);
        result.Exception.ShouldBe(providerError);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutAttachments_SendsTextOnlyMessage()
    {
        // Arrange
        var messages = SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings { AgentId = TestAgentId, Message = "Hello" });

        // Act
        await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        messages.ShouldHaveSingleItem().Contents.ShouldHaveSingleItem().ShouldBeOfType<TextContent>();
    }

    [Fact]
    public async Task ExecuteAsync_WithAttachments_AttachesEachFileAfterTheTextWithItsMediaName()
    {
        // Arrange
        var firstKey = Guid.NewGuid();
        var secondKey = Guid.NewGuid();
        SetupMedia(firstKey, "image/png", name: "Pink lamp");
        SetupMedia(secondKey, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", name: "Brief");

        var messages = SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = TestAgentId,
            Message = "Describe these",
            Attachments = $"{firstKey}, umb://media/{secondKey:N}",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        var contents = messages.ShouldHaveSingleItem().Contents;
        contents.Count.ShouldBe(3);
        contents[0].ShouldBeOfType<TextContent>().Text.ShouldBe("Describe these");
        var image = contents[1].ShouldBeOfType<DataContent>();
        image.MediaType.ShouldBe("image/png");
        image.Name.ShouldBe("Pink lamp");
        var document = contents[2].ShouldBeOfType<DataContent>();
        document.MediaType.ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        document.Name.ShouldBe("Brief");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttachmentAccessDenied_FailsWithoutRunningAgent()
    {
        // Arrange
        var mediaKey = Guid.NewGuid();
        SetupMedia(mediaKey, "image/png");
        _authorizerMock.Setup(a => a.AuthorizeMediaAsync(mediaKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutomationAuthorizationResult.Fail("No access"));

        SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings { AgentId = TestAgentId, Message = "Hi", Attachments = mediaKey.ToString() });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
        VerifyAgentNeverRun();
        _mediaResolverMock.Verify(
            r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttachmentCannotBeResolved_FailsWithoutRunningAgent()
    {
        // Arrange
        var mediaKey = Guid.NewGuid();
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(mediaKey, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings { AgentId = TestAgentId, Message = "Hi", Attachments = mediaKey.ToString() });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        VerifyAgentNeverRun();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttachmentsExceedCombinedSizeLimit_FailsWithoutRunningAgent()
    {
        // Arrange
        var firstKey = Guid.NewGuid();
        var secondKey = Guid.NewGuid();
        var halfPlusOne = (int)(RunAgentAction.MaxTotalAttachmentBytes / 2) + 1;
        SetupMedia(firstKey, "audio/mpeg", size: halfPlusOne);
        SetupMedia(secondKey, "audio/mpeg", size: halfPlusOne);

        SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings
        {
            AgentId = TestAgentId,
            Message = "Hi",
            Attachments = $"{firstKey}\n{secondKey}",
        });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        VerifyAgentNeverRun();
    }

    [Theory]
    [InlineData("not-a-media-reference")]
    [InlineData("/media/1234/photo.png")]
    public async Task ExecuteAsync_WithInvalidAttachmentReference_FailsWithoutRunningAgent(string attachments)
    {
        // Arrange
        SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings { AgentId = TestAgentId, Message = "Hi", Attachments = attachments });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        VerifyAgentNeverRun();
    }

    [Fact]
    public async Task ExecuteAsync_WithTooManyAttachments_FailsWithoutRunningAgent()
    {
        // Arrange
        var attachments = string.Join(",", Enumerable.Range(0, RunAgentAction.MaxAttachments + 1).Select(_ => Guid.NewGuid()));

        SetupAgentCapturingMessages();
        var action = CreateAction();
        var context = CreateContext(new RunAgentSettings { AgentId = TestAgentId, Message = "Hi", Attachments = attachments });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        VerifyAgentNeverRun();
    }

    private List<ChatMessage> SetupAgentCapturingMessages()
    {
        var agent = new AIAgent { Alias = "test-agent", Name = "Test Agent" };
        var captured = new List<ChatMessage>();

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _agentServiceMock
            .Setup(s => s.RunAgentAsync(
                agent.Id,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<AIAgentExecutionOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<ChatMessage>, AIAgentExecutionOptions?, CancellationToken>(
                (_, msgs, _, _) => captured.AddRange(msgs))
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        return captured;
    }

    private void SetupMedia(Guid mediaKey, string mediaType, string name = "Test media", int size = 3)
    {
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(mediaKey, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = new byte[size], MediaType = mediaType, MediaKey = mediaKey });

        var media = new Mock<IMedia>();
        media.SetupGet(m => m.Name).Returns(name);
        _mediaServiceMock.Setup(m => m.GetById(mediaKey)).Returns(media.Object);
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
            _infrastructure,
            _agentServiceMock.Object,
            _userServiceMock.Object,
            _mediaServiceMock.Object,
            _mediaResolverMock.Object,
            _authorizerMock.Object,
            _loggerMock.Object);

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
