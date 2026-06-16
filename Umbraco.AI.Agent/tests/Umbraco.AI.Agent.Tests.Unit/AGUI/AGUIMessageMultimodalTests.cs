using System.Text.Json;
using Shouldly;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIMessageMultimodalTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    #region Backward Compatibility

    [Fact]
    public void Deserialize_StringContent_SetsContentProperty()
    {
        // Arrange
        var json = """{"id":"msg-1","role":"user","content":"Hello world"}""";

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.Content.ShouldBe("Hello world");
        message.ContentParts.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_NullContent_SetsContentToNull()
    {
        // Arrange
        var json = """{"id":"msg-1","role":"user","content":null}""";

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.Content.ShouldBeNull();
        message.ContentParts.ShouldBeNull();
    }

    [Fact]
    public void Serialize_StringContent_WritesAsString()
    {
        // Arrange
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            Content = "Hello world"
        };

        // Act
        var json = JsonSerializer.Serialize(message, Options);

        // Assert
        json.ShouldContain("\"content\":\"Hello world\"");
    }

    [Fact]
    public void RoundTrip_PlainTextMessage_PreservesContent()
    {
        // Arrange
        var original = new AGUIMessage
        {
            Id = "msg-1",
            Role = AGUIMessageRole.User,
            Content = "Hello world"
        };

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe("msg-1");
        deserialized.Role.ShouldBe(AGUIMessageRole.User);
        deserialized.Content.ShouldBe("Hello world");
        deserialized.ContentParts.ShouldBeNull();
    }

    #endregion

    #region Multimodal Content Array

    [Fact]
    public void Deserialize_ContentArray_SetsContentParts()
    {
        // Arrange
        var json = """
        {
            "id": "msg-1",
            "role": "user",
            "content": [
                {"type": "text", "text": "What's in this image?"},
                {"type": "image", "source": {"type": "data", "value": "iVBORw0KGgo=", "mimeType": "image/png"}, "metadata": {"filename": "screenshot.png"}}
            ]
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.ContentParts.ShouldNotBeNull();
        message.ContentParts.Count.ShouldBe(2);

        var textPart = message.ContentParts[0].ShouldBeOfType<AGUITextInputContent>();
        textPart.Text.ShouldBe("What's in this image?");

        var imagePart = message.ContentParts[1].ShouldBeOfType<AGUIImageInputContent>();
        var dataSource = imagePart.Source.ShouldBeOfType<AGUIInputContentDataSource>();
        dataSource.MimeType.ShouldBe("image/png");
        dataSource.Value.ShouldBe("iVBORw0KGgo=");
        imagePart.Metadata.ShouldNotBeNull();
        imagePart.Metadata!["filename"]!.ToString().ShouldBe("screenshot.png");
    }

    [Fact]
    public void Deserialize_ContentArray_DerivesTextFromParts()
    {
        // Arrange
        var json = """
        {
            "id": "msg-1",
            "role": "user",
            "content": [
                {"type": "text", "text": "Hello "},
                {"type": "image", "source": {"type": "data", "value": "abc=", "mimeType": "image/png"}},
                {"type": "text", "text": "world"}
            ]
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.Content.ShouldBe("Hello world");
    }

    [Fact]
    public void Serialize_ContentParts_WritesAsArray()
    {
        // Arrange
        var message = new AGUIMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUITextInputContent { Text = "Check this file" },
                new AGUIDocumentInputContent
                {
                    Source = new AGUIInputContentUrlSource { Value = "https://server/file/file-123", MimeType = "application/pdf" },
                    Metadata = new Dictionary<string, object?>
                    {
                        ["filename"] = "report.pdf",
                        ["fileId"] = "file-123"
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(message, Options);
        var doc = JsonDocument.Parse(json);
        var contentElement = doc.RootElement.GetProperty("content");

        // Assert
        contentElement.ValueKind.ShouldBe(JsonValueKind.Array);
        contentElement.GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public void RoundTrip_MultimodalMessage_PreservesContentParts()
    {
        // Arrange
        var original = new AGUIMessage
        {
            Id = "msg-2",
            Role = AGUIMessageRole.User,
            Content = "Check this",
            ContentParts = new List<AGUIInputContent>
            {
                new AGUITextInputContent { Text = "Check this" },
                new AGUIImageInputContent
                {
                    Source = new AGUIInputContentDataSource { Value = "base64data", MimeType = "image/jpeg" },
                    Metadata = new Dictionary<string, object?> { ["filename"] = "photo.jpg" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.ContentParts.ShouldNotBeNull();
        deserialized.ContentParts.Count.ShouldBe(2);

        var textPart = deserialized.ContentParts[0].ShouldBeOfType<AGUITextInputContent>();
        textPart.Text.ShouldBe("Check this");

        var imagePart = deserialized.ContentParts[1].ShouldBeOfType<AGUIImageInputContent>();
        var dataSource = imagePart.Source.ShouldBeOfType<AGUIInputContentDataSource>();
        dataSource.MimeType.ShouldBe("image/jpeg");
        dataSource.Value.ShouldBe("base64data");
        imagePart.Metadata.ShouldNotBeNull();
        imagePart.Metadata!["filename"]!.ToString().ShouldBe("photo.jpg");
    }

    [Fact]
    public void Deserialize_BinaryWithIdReference_PreservesId()
    {
        // Arrange — represents a message from a snapshot where base64 has been replaced with a URL reference
        // and the file id lives in metadata.
        var json = """
        {
            "id": "msg-1",
            "role": "user",
            "content": [
                {"type": "text", "text": "Analyze this"},
                {"type": "image", "source": {"type": "url", "value": "https://server/file/file-abc123", "mimeType": "image/png"}, "metadata": {"filename": "chart.png", "fileId": "file-abc123"}}
            ]
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        var imagePart = message!.ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        imagePart.Source.ShouldBeOfType<AGUIInputContentUrlSource>();
        imagePart.Metadata.ShouldNotBeNull();
        imagePart.Metadata!["fileId"]!.ToString().ShouldBe("file-abc123");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_EmptyContentArray_SetsEmptyContentParts()
    {
        // Arrange
        var json = """{"id":"msg-1","role":"user","content":[]}""";

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.ContentParts.ShouldNotBeNull();
        message.ContentParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Deserialize_MessageWithToolCalls_PreservesToolCalls()
    {
        // Arrange — verify multimodal doesn't break tool call handling
        var json = """
        {
            "id": "msg-1",
            "role": "assistant",
            "content": "Let me search for that",
            "toolCalls": [{"id": "call-1", "type": "function", "function": {"name": "search", "arguments": "{}"}}]
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        message.ShouldNotBeNull();
        message.Content.ShouldBe("Let me search for that");
        message.ToolCalls.ShouldNotBeNull();
        message.ToolCalls!.Count().ShouldBe(1);
    }

    #endregion
}
