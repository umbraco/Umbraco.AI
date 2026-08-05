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
            new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Hello" },
            new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.Assistant, Content = "Hi there!" }
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
        var message = new AGUIMessage { Id = Guid.NewGuid().ToString(), Role = role, Content = "Test content" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Text.ShouldBe("Test content");
    }

    [Fact]
    public void ConvertToChatMessage_UserRole_ConvertsToUserChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Hello" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.User);
    }

    [Fact]
    public void ConvertToChatMessage_AssistantRole_ConvertsToAssistantChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.Assistant, Content = "Hi" };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.Assistant);
    }

    [Fact]
    public void ConvertToChatMessage_DeveloperRole_MapsToSystemChatRole()
    {
        // Arrange
        var message = new AGUIMessage { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.Developer, Content = "Dev message" };

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
            Id = Guid.NewGuid().ToString(),
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
            Id = Guid.NewGuid().ToString(),
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
        var message = new AGUIMessage { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = null };

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

    #region Multimodal Content Tests

    [Fact]
    public void ConvertToChatMessage_WithContentParts_CreatesMultimodalContent()
    {
        // Arrange
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            Content = "What's in this image?",
            ContentParts = new List<AGUIInputContent>
            {
                new AGUITextInputContent { Text = "What's in this image?" },
                // Resolved bytes attached the same way AGUIFileProcessor would after resolving a stored file.
                new AGUIImageInputContent
                {
                    Source = new AGUIInputContentUrlSource { Value = "https://server/file/file-abc", MimeType = "image/png" },
                    Metadata = new Dictionary<string, object?>
                    {
                        ["fileId"] = "file-abc",
                        ["__resolvedData"] = imageBytes
                    }
                }
            }
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Role.ShouldBe(ChatRole.User);
        result.Contents.ShouldNotBeNull();
        result.Contents.Count.ShouldBe(2);

        var textContent = result.Contents[0].ShouldBeOfType<TextContent>();
        textContent.Text.ShouldBe("What's in this image?");

        var dataContent = result.Contents[1].ShouldBeOfType<DataContent>();
        dataContent.MediaType.ShouldBe("image/png");
        dataContent.Data.ToArray().ShouldBe(imageBytes);
    }

    [Fact]
    public void ConvertToChatMessage_WithBase64FallbackData_DecodesBase64()
    {
        // Arrange
        var base64 = Convert.ToBase64String(new byte[] { 10, 20, 30 });
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUIDocumentInputContent
                {
                    Source = new AGUIInputContentDataSource { Value = base64, MimeType = "application/pdf" }
                }
            }
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        var dataContent = result.Contents.OfType<DataContent>().Single();
        dataContent.MediaType.ShouldBe("application/pdf");
        dataContent.Data.ToArray().ShouldBe(new byte[] { 10, 20, 30 });
    }

    [Fact]
    public void ConvertToChatMessage_WithFilenameMetadata_SetsDataContentName()
    {
        // Arrange
        var base64 = Convert.ToBase64String(new byte[] { 10, 20, 30 });
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUIDocumentInputContent
                {
                    Source = new AGUIInputContentDataSource { Value = base64, MimeType = "text/csv" },
                    Metadata = new Dictionary<string, object?> { ["filename"] = "example.csv" }
                }
            }
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Contents.OfType<DataContent>().Single().Name.ShouldBe("example.csv");
    }

    [Fact]
    public void ConvertToChatMessage_WithDeserializedFilenameMetadata_SetsDataContentName()
    {
        // Arrange — metadata deserialized from the wire holds JsonElement values, not strings.
        var base64 = Convert.ToBase64String(new byte[] { 10, 20, 30 });
        var json = """
        {
            "id": "msg-1",
            "role": "user",
            "content": [
                {"type": "document", "source": {"type": "data", "value": "BASE64", "mimeType": "text/csv"}, "metadata": {"filename": "example.csv"}}
            ]
        }
        """.Replace("BASE64", base64);
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Act
        var result = _converter.ConvertToChatMessage(message!);

        // Assert
        result.Contents.OfType<DataContent>().Single().Name.ShouldBe("example.csv");
    }

    [Fact]
    public void ConvertToChatMessage_WithoutFilenameMetadata_LeavesDataContentNameNull()
    {
        // Arrange
        var base64 = Convert.ToBase64String(new byte[] { 10, 20, 30 });
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUIDocumentInputContent
                {
                    Source = new AGUIInputContentDataSource { Value = base64, MimeType = "text/csv" }
                }
            }
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        result.Contents.OfType<DataContent>().Single().Name.ShouldBeNull();
    }

    [Fact]
    public void ConvertToChatMessage_WithUnresolvableUrlSource_CreatesUriContent()
    {
        // Arrange — an external URL we never stored, so there are no resolved bytes to attach.
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUIImageInputContent
                {
                    Source = new AGUIInputContentUrlSource { Value = "https://example.com/chart.png", MimeType = "image/png" }
                }
            }
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert
        var uriContent = result.Contents.OfType<UriContent>().Single();
        uriContent.Uri.ToString().ShouldBe("https://example.com/chart.png");
        uriContent.MediaType.ShouldBe("image/png");
    }

    [Fact]
    public void ConvertToChatMessage_EmptyContentParts_FallsBackToContent()
    {
        // Arrange
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            Content = "Plain text",
            ContentParts = new List<AGUIInputContent>()
        };

        // Act
        var result = _converter.ConvertToChatMessage(message);

        // Assert — empty content parts falls back to plain text path
        result.Text.ShouldBe("Plain text");
    }

    [Fact]
    public void ConvertFromChatMessage_WithDataContent_CreatesContentParts()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Describe this"),
            new DataContent(new byte[] { 1, 2, 3 }, "image/jpeg")
        };
        var chatMessage = new ChatMessage(ChatRole.User, contents);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.ContentParts.ShouldNotBeNull();
        result.ContentParts.Count.ShouldBe(2);

        var textPart = result.ContentParts[0].ShouldBeOfType<AGUITextInputContent>();
        textPart.Text.ShouldBe("Describe this");

        // image/jpeg is classified as Image variant via the factory.
        var binaryPart = result.ContentParts[1].ShouldBeOfType<AGUIImageInputContent>();
        var dataSource = binaryPart.Source.ShouldBeOfType<AGUIInputContentDataSource>();
        dataSource.MimeType.ShouldBe("image/jpeg");
        dataSource.Value.ShouldNotBeNull();
    }

    [Fact]
    public void ConvertFromChatMessage_WithNamedDataContent_SetsFilenameMetadata()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new DataContent(new byte[] { 1, 2, 3 }, "text/csv") { Name = "example.csv" }
        };
        var chatMessage = new ChatMessage(ChatRole.User, contents);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        var part = result.ContentParts!.Single().ShouldBeOfType<AGUIDocumentInputContent>();
        part.Metadata.ShouldNotBeNull();
        part.Metadata!["filename"].ShouldBe("example.csv");
    }

    [Fact]
    public void ConvertFromChatMessage_WithUnnamedDataContent_LeavesMetadataNull()
    {
        // Arrange
        var chatMessage = new ChatMessage(ChatRole.User, [new DataContent(new byte[] { 1, 2, 3 }, "text/csv")]);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.ContentParts!.Single().ShouldBeOfType<AGUIDocumentInputContent>().Metadata.ShouldBeNull();
    }

    [Fact]
    public void ConvertFromChatMessage_WithUriContent_CreatesUrlSourcePart()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Describe this"),
            new UriContent(new Uri("https://example.com/chart.png"), "image/png")
        };
        var chatMessage = new ChatMessage(ChatRole.User, contents);

        // Act
        var result = _converter.ConvertFromChatMessage(chatMessage);

        // Assert
        result.ContentParts.ShouldNotBeNull();
        result.ContentParts.Count.ShouldBe(2);

        var urlPart = result.ContentParts[1].ShouldBeOfType<AGUIImageInputContent>();
        var urlSource = urlPart.Source.ShouldBeOfType<AGUIInputContentUrlSource>();
        urlSource.Value.ShouldBe("https://example.com/chart.png");
        urlSource.MimeType.ShouldBe("image/png");
    }

    [Fact]
    public void ConvertFromChatMessage_RoundTripsFilenameThroughBothDirections()
    {
        // Arrange
        var original = new ChatMessage(ChatRole.User, [new DataContent(new byte[] { 1, 2, 3 }, "text/csv") { Name = "example.csv" }])
        {
            MessageId = "msg-1"
        };

        // Act
        var aguiMessage = _converter.ConvertFromChatMessage(original);
        var roundTripped = _converter.ConvertToChatMessage(aguiMessage);

        // Assert
        roundTripped.Contents.OfType<DataContent>().Single().Name.ShouldBe("example.csv");
    }

    #endregion
}
