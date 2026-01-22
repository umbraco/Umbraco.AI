using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.AuditLog;

namespace Umbraco.Ai.Tests.Unit.Formatters;

public class AiChatMessageFormatterTests
{
    #region Single Message Formatting

    [Fact]
    public void FormatChatMessage_WithTextContent_FormatsCorrectly()
    {
        // Arrange
        var message = new ChatMessage(ChatRole.Assistant, "Hello, world!");

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldBe("[assistant] Hello, world!");
    }

    [Fact]
    public void FormatChatMessage_WithFunctionCallContent_FormatsToolCall()
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

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldContain("[assistant] Let me check the weather.");
        result.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
    }

    [Fact]
    public void FormatChatMessage_WithOnlyFunctionCall_FormatsCorrectly()
    {
        // Arrange
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "ping",
            arguments: new Dictionary<string, object?>());

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldContain("[tool_call:tc_001] ping({})");
    }

    [Fact]
    public void FormatChatMessage_WithMultipleFunctionCalls_FormatsAll()
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
            new TextContent("I'll check both."),
            call1,
            call2
        });

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldContain("[tool_call:tc_001] get_weather");
        result.ShouldContain("[tool_call:tc_002] get_time");
    }

    [Fact]
    public void FormatChatMessage_WithEmptyMessage_FormatsRoleOnly()
    {
        // Arrange
        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent>());

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldBe("[assistant]");
    }

    #endregion

    #region Multiple Messages Formatting

    [Fact]
    public void FormatChatMessages_WithMultipleMessages_FormatsSeparatedByNewlines()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello!"),
            new(ChatRole.Assistant, "Hi there! How can I help you?")
        };

        // Act
        var result = AiChatMessageFormatter.FormatChatMessages(messages);

        // Assert
        result.ShouldContain("[system] You are a helpful assistant.");
        result.ShouldContain("[user] Hello!");
        result.ShouldContain("[assistant] Hi there! How can I help you?");

        // Verify they're on separate lines
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void FormatChatMessages_WithToolCallsAndResults_FormatsComplete()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
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
        var result = AiChatMessageFormatter.FormatChatMessages(messages);

        // Assert
        result.ShouldContain("[user] What's the weather in London?");
        result.ShouldContain("[assistant] Let me check the weather for you.");
        result.ShouldContain("[tool_call:tc_001] get_weather({\"city\":\"London\"})");
        result.ShouldContain("[tool:tc_001] -> {\"temperature\":15,\"condition\":\"partly cloudy\"}");
        result.ShouldContain("[assistant] The weather in London is 15 degrees and partly cloudy.");
    }

    #endregion

    #region Truncation

    [Fact]
    public void FormatChatMessage_WithLargeArguments_Truncates()
    {
        // Arrange
        var largeValue = new string('x', 600);
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "process",
            arguments: new Dictionary<string, object?> { ["data"] = largeValue });

        var message = new ChatMessage(ChatRole.Assistant, new List<AIContent> { functionCall });

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldContain("(truncated,");
        result.ShouldContain("chars)");
    }

    [Fact]
    public void FormatChatMessage_WithLargeResult_Truncates()
    {
        // Arrange
        var largeResult = new string('y', 1200);
        var functionResult = new FunctionResultContent(
            callId: "tc_001",
            result: largeResult);

        var message = new ChatMessage(ChatRole.Tool, new List<AIContent> { functionResult });

        // Act
        var result = AiChatMessageFormatter.FormatChatMessage(message);

        // Assert
        result.ShouldContain("(truncated,");
        result.ShouldContain("chars)");
    }

    #endregion
}
