using System.Text.Json;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIMessageConverterTests
{
    private readonly AGUIMessageConverter _converter = new();

    #region ConvertToChatMessages Tests

    [Fact]
    public void ConvertToChatMessages_WithNullMessages_ReturnsEmptyList()
    {
        // Act
        var result = _converter.ConvertToChatMessages(null);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertToChatMessages_WithMessages_ConvertsAllMessages()
    {
        // Arrange
        var messages = new List<AGUIMessage>
        {
            new() { Role = AGUIMessageRole.User, Content = "Hello" },
            new() { Role = AGUIMessageRole.Assistant, Content = "Hi there!" }
        };

        // Act
        var result = _converter.ConvertToChatMessages(messages);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Role.ShouldBe(ChatRole.User);
        result[0].Text.ShouldBe("Hello");
        result[1].Role.ShouldBe(ChatRole.Assistant);
        result[1].Text.ShouldBe("Hi there!");
    }

    #endregion

    #region ConvertToChatMessage Tests

    [Theory]
    [InlineData(AGUIMessageRole.User)]
    [InlineData(AGUIMessageRole.Assistant)]
    [InlineData(AGUIMessageRole.System)]
    public void ConvertToChatMessage_WithSimpleMessage_ConvertsRole(AGUIMessageRole role)
    {
        // Arrange
        var message = new AGUIMessage { Role = role, Content = "Test content" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Text.ShouldBe("Test content");
    }

    [Fact]
    public void ConvertToChatMessage_UserRole_ConvertsToUserChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Role = AGUIMessageRole.User, Content = "Hello" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public void ConvertToChatMessage_AssistantRole_ConvertsToAssistantChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Role = AGUIMessageRole.Assistant, Content = "Hi" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.Assistant);
    }

    [Fact]
    public void ConvertToChatMessage_DeveloperRole_MapsToSystemChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Role = AGUIMessageRole.Developer, Content = "Dev message" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.System);
    }

    [Fact]
    public void ConvertToChatMessage_WithToolCalls_CreatesFunctionCallContent()
    {
        // Arrange
        var message = new AGUIMessage
        {
            Role = AGUIMessageRole.Assistant,
            Content = "Let me help with that",
            ToolCalls =
            [
                new AGUIToolCall
                {
                    Id = "call-123",
                    Type = "function",
                    Function = new AGUIFunctionCall
                    {
                        Name = "get_weather",
                        Arguments = "{\"city\":\"London\"}"
                    }
                }
            ]
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.Assistant);
        result.Contents.ShouldNotBeNull();

        var textContent = result.Contents.OfType<TextContent>().FirstOrDefault();
        textContent.ShouldNotBeNull();
        textContent.Text.ShouldBe("Let me help with that");

        var functionCall = result.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        functionCall.ShouldNotBeNull();
        functionCall.CallId.ShouldBe("call-123");
        functionCall.Name.ShouldBe("get_weather");
    }

    [Fact]
    public void ConvertToChatMessage_ToolResultMessage_CreatesFunctionResultContent()
    {
        // Arrange
        var message = new AGUIMessage
        {
            Role = AGUIMessageRole.Tool,
            ToolCallId = "call-123",
            Content = "{\"temperature\": 20}"
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.Tool);
        var functionResult = result.Contents.OfType<FunctionResultContent>().FirstOrDefault();
        functionResult.ShouldNotBeNull();
        functionResult.CallId.ShouldBe("call-123");
    }

    [Fact]
    public void ConvertToChatMessage_WithNullContent_SetsEmptyString()
    {
        // Arrange
        var message = new AGUIMessage { Role = AGUIMessageRole.User, Content = null };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Text.ShouldBe(string.Empty);
    }

    #endregion

    #region ConvertFromChatMessage Tests

    [Fact]
    public void ConvertFromChatMessage_SimpleUserMessage_ConvertsCorrectly()
    {
        // Arrange
        var chatMessage = new ChatMessage(ChatRole.User, "Hello world");

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.Role.ShouldBe(AGUIMessageRole.User);
        result.Content.ShouldBe("Hello world");
    }

    [Fact]
    public void ConvertFromChatMessage_AssistantMessage_ConvertsCorrectly()
    {
        // Arrange
        var chatMessage = new ChatMessage(ChatRole.Assistant, "Hi there");

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.Role.ShouldBe(AGUIMessageRole.Assistant);
        result.Content.ShouldBe("Hi there");
    }

    [Fact]
    public void ConvertFromChatMessage_SystemMessage_ConvertsCorrectly()
    {
        // Arrange
        var chatMessage = new ChatMessage(ChatRole.System, "You are helpful");

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.Role.ShouldBe(AGUIMessageRole.System);
    }

    [Fact]
    public void ConvertFromChatMessage_WithFunctionCall_IncludesToolCalls()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("I'll help with that"),
            new FunctionCallContent("call-abc", "search", new Dictionary<string, object?> { ["query"] = "test" })
        };
        var chatMessage = new ChatMessage(ChatRole.Assistant, contents);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.Role.ShouldBe(AGUIMessageRole.Assistant);
        result.ToolCalls.ShouldNotBeNull();
        var toolCalls = result.ToolCalls.ToList();
        toolCalls.Count.ShouldBe(1);
        toolCalls[0].Id.ShouldBe("call-abc");
        toolCalls[0].Function.Name.ShouldBe("search");
    }

    [Fact]
    public void ConvertFromChatMessage_WithFunctionResult_SetsToolCallId()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new FunctionResultContent("call-xyz", "result data")
        };
        var chatMessage = new ChatMessage(ChatRole.Tool, contents);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.Role.ShouldBe(AGUIMessageRole.Tool);
        result.ToolCallId.ShouldBe("call-xyz");
    }

    #endregion
}
