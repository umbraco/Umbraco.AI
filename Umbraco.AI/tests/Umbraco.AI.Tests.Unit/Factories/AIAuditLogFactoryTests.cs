using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Tests.Unit.Factories;

public class AIAuditLogFactoryTests
{
    private readonly Mock<IOptionsMonitor<AIAuditLogOptions>> _optionsMock;
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessorMock;
    private readonly Mock<ILogger<AIAuditLogFactory>> _loggerMock;

    public AIAuditLogFactoryTests()
    {
        _optionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        _securityAccessorMock = new Mock<IBackOfficeSecurityAccessor>();
        _loggerMock = new Mock<ILogger<AIAuditLogFactory>>();

        // Default options with prompt persistence enabled
        var options = new AIAuditLogOptions
        {
            PersistPrompts = true,
            RedactionPatterns = new List<string>()
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(options);
    }

    private AIAuditLogFactory CreateFactory()
    {
        return new AIAuditLogFactory(
            _optionsMock.Object,
            _securityAccessorMock.Object,
            _loggerMock.Object);
    }

    private static AIAuditContext CreateContext(
        object? prompt,
        AICapability capability = AICapability.Chat)
    {
        return new AIAuditContext
        {
            Capability = capability,
            ProfileId = Guid.NewGuid(),
            ProfileAlias = "test-profile",
            ProviderId = "test-provider",
            ModelId = "test-model",
            Prompt = prompt
        };
    }

    #region Text Content Formatting

    [Fact]
    public void Create_WithTextContent_FormatsCorrectly()
    {
        // Arrange
        var factory = CreateFactory();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello!")
        };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[system] You are a helpful assistant.");
        auditLog.PromptSnapshot.ShouldContain("[user] Hello!");
    }

    [Fact]
    public void Create_WithEmptyMessage_FormatsRoleOnly()
    {
        // Arrange
        var factory = CreateFactory();
        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent>());
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldBe("[assistant]");
    }

    #endregion

    #region Function Call Content

    [Fact]
    public void Create_WithFunctionCallContent_FormatsToolCallCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "get_weather",
            arguments: new Dictionary<string, object?> { ["city"] = "London" });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            new TextContent("Let me check the weather."),
            functionCall
        });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[assistant] Let me check the weather.");
        auditLog.PromptSnapshot.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
    }

    [Fact]
    public void Create_WithMultipleFunctionCalls_FormatsAllToolCalls()
    {
        // Arrange
        var factory = CreateFactory();

        var call1 = new FunctionCallContent(
            callId: "tc_001",
            name: "get_weather",
            arguments: new Dictionary<string, object?> { ["city"] = "London" });

        var call2 = new FunctionCallContent(
            callId: "tc_002",
            name: "get_time",
            arguments: new Dictionary<string, object?> { ["timezone"] = "UTC" });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            new TextContent("Let me check both."),
            call1,
            call2
        });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[tool_call:tc_001] get_weather");
        auditLog.PromptSnapshot.ShouldContain("[tool_call:tc_002] get_time");
    }

    [Fact]
    public void Create_WithEmptyArguments_FormatsAsEmptyJson()
    {
        // Arrange
        var factory = CreateFactory();

        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "ping",
            arguments: new Dictionary<string, object?>());

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[tool_call:tc_001] ping({})");
    }

    #endregion

    #region Function Result Content

    [Fact]
    public void Create_WithFunctionResultContent_FormatsToolResultCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: "{\"temperature\":15,\"condition\":\"partly cloudy\"}");

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[tool:tc_001] -> {\"temperature\":15,\"condition\":\"partly cloudy\"}");
    }

    [Fact]
    public void Create_WithNullResult_FormatsAsNull()
    {
        // Arrange
        var factory = CreateFactory();

        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: null);

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[tool:tc_001] -> (null)");
    }

    [Fact]
    public void Create_WithObjectResult_SerializesToJson()
    {
        // Arrange
        var factory = CreateFactory();

        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: new { value = 42, name = "test" });

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[tool:tc_001] -> {\"value\":42,\"name\":\"test\"}");
    }

    #endregion

    #region Truncation

    [Fact]
    public void Create_WithLargeArguments_Truncates()
    {
        // Arrange
        var factory = CreateFactory();

        // Create arguments that exceed 500 characters
        var largeValue = new string('x', 600);
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "process",
            arguments: new Dictionary<string, object?> { ["data"] = largeValue });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("(truncated,");
        auditLog.PromptSnapshot.ShouldContain("chars)");
    }

    [Fact]
    public void Create_WithLargeResult_Truncates()
    {
        // Arrange
        var factory = CreateFactory();

        // Create result that exceeds 1000 characters
        var largeResult = new string('y', 1200);
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: largeResult);

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("(truncated,");
        auditLog.PromptSnapshot.ShouldContain("chars)");
    }

    #endregion

    #region Data Content

    [Fact]
    public void Create_WithDataContent_FormatsMimeTypeAndSize()
    {
        // Arrange
        var factory = CreateFactory();

        var dataContent = new DataContent(
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, // PNG magic bytes
            "image/png");

        var message = new ChatMessage(ChatRole.User, new List<AIContent>
        {
            new TextContent("Here is an image:"),
            dataContent
        });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[user] Here is an image:");
        auditLog.PromptSnapshot.ShouldContain("[data:image/png] (4 bytes)");
    }

    #endregion

    #region Mixed Content Types

    [Fact]
    public void Create_WithFullConversation_FormatsCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant with weather tools."),
            new(ChatRole.User, "What's the weather in London?"),
            new(ChatRole.Assistant, new List<AIContent>
            {
                new TextContent("Let me check the weather for you."),
                new FunctionCallContent(
                    callId: "tc_001",
                    name: "get_weather",
                    arguments: new Dictionary<string, object?> { ["city"] = "London" })
            }),
            new(ChatRole.Tool, new List<AIContent>
            {
                new FunctionResultContent(
                    callId: "tc_001",
                    result: "{\"temperature\":15,\"condition\":\"partly cloudy\"}")
            }),
            new(ChatRole.Assistant, "The weather in London is 15 degrees and partly cloudy.")
        };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[system] You are a helpful assistant with weather tools.");
        auditLog.PromptSnapshot.ShouldContain("[user] What's the weather in London?");
        auditLog.PromptSnapshot.ShouldContain("[assistant] Let me check the weather for you.");
        auditLog.PromptSnapshot.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
        auditLog.PromptSnapshot.ShouldContain("[tool:tc_001] -> {\"temperature\":15,\"condition\":\"partly cloudy\"}");
        auditLog.PromptSnapshot.ShouldContain("[assistant] The weather in London is 15 degrees and partly cloudy.");
    }

    #endregion

    #region Redaction

    [Fact]
    public void Create_WithRedactionPattern_RedactsToolContent()
    {
        // Arrange
        var options = new AIAuditLogOptions
        {
            PersistPrompts = true,
            RedactionPatterns = new List<string> { "secret-\\w+" }
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(options);

        var factory = CreateFactory();

        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "authenticate",
            arguments: new Dictionary<string, object?> { ["token"] = "secret-abc123" });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[REDACTED]");
        auditLog.PromptSnapshot.ShouldNotContain("secret-abc123");
    }

    [Fact]
    public void Create_WithRedactionPattern_RedactsToolResults()
    {
        // Arrange
        var options = new AIAuditLogOptions
        {
            PersistPrompts = true,
            RedactionPatterns = new List<string> { "api_key_\\w+" }
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(options);

        var factory = CreateFactory();

        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: "{\"key\":\"api_key_xyz789\"}");

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[REDACTED]");
        auditLog.PromptSnapshot.ShouldNotContain("api_key_xyz789");
    }

    #endregion

    #region Prompt Persistence Disabled

    [Fact]
    public void Create_WithPromptPersistenceDisabled_DoesNotCapturePrompt()
    {
        // Arrange
        var options = new AIAuditLogOptions
        {
            PersistPrompts = false,
            RedactionPatterns = new List<string>()
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(options);

        var factory = CreateFactory();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello!")
        };
        var context = CreateContext(messages);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldBeNull();
    }

    #endregion

    #region Embedding Capability

    [Fact]
    public void Create_WithEmbeddingCapability_FormatsCorrectly()
    {
        // Arrange
        var factory = CreateFactory();
        var values = new List<string> { "First text", "Second text", "Third text" };
        var context = CreateContext(values, AICapability.Embedding);

        // Act
        var auditLog = factory.Create(context);

        // Assert
        auditLog.PromptSnapshot.ShouldNotBeNull();
        auditLog.PromptSnapshot.ShouldContain("[0] First text");
        auditLog.PromptSnapshot.ShouldContain("[1] Second text");
        auditLog.PromptSnapshot.ShouldContain("[2] Third text");
    }

    #endregion
}
