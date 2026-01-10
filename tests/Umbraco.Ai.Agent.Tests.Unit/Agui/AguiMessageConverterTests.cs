using System.Text.Json;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agui.Models;
using Xunit;

namespace Umbraco.Ai.Agent.Tests.Unit.Agui;

public class AguiMessageConverterTests
{
    private readonly AguiMessageConverter _converter = new();

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
        var messages = new List<AguiMessage>
        {
            new() { Role = AguiMessageRole.User, Content = "Hello" },
            new() { Role = AguiMessageRole.Assistant, Content = "Hi there!" }
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
    [InlineData(AguiMessageRole.User)]
    [InlineData(AguiMessageRole.Assistant)]
    [InlineData(AguiMessageRole.System)]
    public void ConvertToChatMessage_WithSimpleMessage_ConvertsRole(AguiMessageRole role)
    {
        // Arrange
        var message = new AguiMessage { Role = role, Content = "Test content" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Text.ShouldBe("Test content");
    }

    [Fact]
    public void ConvertToChatMessage_UserRole_ConvertsToUserChatRole()
    {
        // Arrange
        var message = new AguiMessage { Role = AguiMessageRole.User, Content = "Hello" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public void ConvertToChatMessage_AssistantRole_ConvertsToAssistantChatRole()
    {
        // Arrange
        var message = new AguiMessage { Role = AguiMessageRole.Assistant, Content = "Hi" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.Assistant);
    }

    [Fact]
    public void ConvertToChatMessage_DeveloperRole_MapsToSystemChatRole()
    {
        // Arrange
        var message = new AguiMessage { Role = AguiMessageRole.Developer, Content = "Dev message" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.System);
    }

    [Fact]
    public void ConvertToChatMessage_WithToolCalls_CreatesFunctionCallContent()
    {
        // Arrange
        var message = new AguiMessage
        {
            Role = AguiMessageRole.Assistant,
            Content = "Let me help with that",
            ToolCalls =
            [
                new AguiToolCall
                {
                    Id = "call-123",
                    Type = "function",
                    Function = new AguiFunctionCall
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
        var message = new AguiMessage
        {
            Role = AguiMessageRole.Tool,
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
        var message = new AguiMessage { Role = AguiMessageRole.User, Content = null };

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
        result.Role.ShouldBe(AguiMessageRole.User);
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
        result.Role.ShouldBe(AguiMessageRole.Assistant);
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
        result.Role.ShouldBe(AguiMessageRole.System);
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
        result.Role.ShouldBe(AguiMessageRole.Assistant);
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
        result.Role.ShouldBe(AguiMessageRole.Tool);
        result.ToolCallId.ShouldBe("call-xyz");
    }

    #endregion
}
