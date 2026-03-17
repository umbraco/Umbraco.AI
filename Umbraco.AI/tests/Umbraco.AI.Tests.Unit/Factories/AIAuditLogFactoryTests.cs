using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Unit.Factories;

public class AIAuditLogFactoryTests
{
    #region Text Content Formatting

    [Fact]
    public void FormatPromptSnapshot_WithTextContent_FormatsCorrectly()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello!")
        };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[system] You are a helpful assistant.");
        snapshot.ShouldContain("[user] Hello!");
    }

    [Fact]
    public void FormatPromptSnapshot_WithEmptyMessage_FormatsRoleOnly()
    {
        // Arrange
        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent>());
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldBe("[assistant]");
    }

    #endregion

    #region Function Call Content

    [Fact]
    public void FormatPromptSnapshot_WithFunctionCallContent_FormatsToolCallCorrectly()
    {
        // Arrange
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

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[assistant] Let me check the weather.");
        snapshot.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
    }

    [Fact]
    public void FormatPromptSnapshot_WithMultipleFunctionCalls_FormatsAllToolCalls()
    {
        // Arrange
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

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[tool_call:tc_001] get_weather");
        snapshot.ShouldContain("[tool_call:tc_002] get_time");
    }

    [Fact]
    public void FormatPromptSnapshot_WithEmptyArguments_FormatsAsEmptyJson()
    {
        // Arrange
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "ping",
            arguments: new Dictionary<string, object?>());

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[tool_call:tc_001] ping({})");
    }

    #endregion

    #region Function Result Content

    [Fact]
    public void FormatPromptSnapshot_WithFunctionResultContent_FormatsToolResultCorrectly()
    {
        // Arrange
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: "{\"temperature\":15,\"condition\":\"partly cloudy\"}");

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[tool:tc_001] -> {\"temperature\":15,\"condition\":\"partly cloudy\"}");
    }

    [Fact]
    public void FormatPromptSnapshot_WithNullResult_FormatsAsNull()
    {
        // Arrange
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: null);

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[tool:tc_001] -> (null)");
    }

    [Fact]
    public void FormatPromptSnapshot_WithObjectResult_SerializesToJson()
    {
        // Arrange
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: new { value = 42, name = "test" });

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[tool:tc_001] -> {\"value\":42,\"name\":\"test\"}");
    }

    #endregion

    #region Truncation

    [Fact]
    public void FormatPromptSnapshot_WithLargeArguments_Truncates()
    {
        // Arrange
        // Create arguments that exceed 500 characters
        var largeValue = new string('x', 600);
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "process",
            arguments: new Dictionary<string, object?> { ["data"] = largeValue });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("(truncated,");
        snapshot.ShouldContain("chars)");
    }

    [Fact]
    public void FormatPromptSnapshot_WithLargeResult_Truncates()
    {
        // Arrange
        // Create result that exceeds 1000 characters
        var largeResult = new string('y', 1200);
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: largeResult);

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("(truncated,");
        snapshot.ShouldContain("chars)");
    }

    #endregion

    #region Data Content

    [Fact]
    public void FormatPromptSnapshot_WithDataContent_FormatsMimeTypeAndSize()
    {
        // Arrange
        var dataContent = new DataContent(
            new byte[] { 0x89, 0x50, 0x4E, 0x47 }, // PNG magic bytes
            "image/png");

        var message = new ChatMessage(ChatRole.User, new List<AIContent>
        {
            new TextContent("Here is an image:"),
            dataContent
        });
        var messages = new List<ChatMessage> { message };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[user] Here is an image:");
        snapshot.ShouldContain("[data:image/png] (4 bytes)");
    }

    #endregion

    #region Mixed Content Types

    [Fact]
    public void FormatPromptSnapshot_WithFullConversation_FormatsCorrectly()
    {
        // Arrange
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

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(messages, AICapability.Chat);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[system] You are a helpful assistant with weather tools.");
        snapshot.ShouldContain("[user] What's the weather in London?");
        snapshot.ShouldContain("[assistant] Let me check the weather for you.");
        snapshot.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
        snapshot.ShouldContain("[tool:tc_001] -> {\"temperature\":15,\"condition\":\"partly cloudy\"}");
        snapshot.ShouldContain("[assistant] The weather in London is 15 degrees and partly cloudy.");
    }

    #endregion

    #region Redaction

    [Fact]
    public void ApplyRedaction_WithPattern_RedactsMatchingContent()
    {
        // Arrange
        var patterns = new List<string> { "secret-\\w+" };
        var logger = NullLogger.Instance;
        var input = "[tool_call:tc_001] authenticate({\"token\":\"secret-abc123\"})";

        // Act
        var result = AIAuditLogRedactor.ApplyRedaction(input, patterns, logger);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("[REDACTED]");
        result.ShouldNotContain("secret-abc123");
    }

    [Fact]
    public void ApplyRedaction_WithPattern_RedactsToolResults()
    {
        // Arrange
        var patterns = new List<string> { "api_key_\\w+" };
        var logger = NullLogger.Instance;
        var input = "[tool:tc_001] -> {\"key\":\"api_key_xyz789\"}";

        // Act
        var result = AIAuditLogRedactor.ApplyRedaction(input, patterns, logger);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("[REDACTED]");
        result.ShouldNotContain("api_key_xyz789");
    }

    #endregion

    #region Null/Empty Handling

    [Fact]
    public void FormatPromptSnapshot_WithNullPrompt_ReturnsNull()
    {
        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(null, AICapability.Chat);

        // Assert
        snapshot.ShouldBeNull();
    }

    #endregion

    #region Embedding Capability

    [Fact]
    public void FormatPromptSnapshot_WithEmbeddingCapability_FormatsCorrectly()
    {
        // Arrange
        var values = new List<string> { "First text", "Second text", "Third text" };

        // Act
        var snapshot = AIAuditLogFactory.FormatPromptSnapshot(values, AICapability.Embedding);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ShouldContain("[0] First text");
        snapshot.ShouldContain("[1] Second text");
        snapshot.ShouldContain("[2] Third text");
    }

    #endregion
}
