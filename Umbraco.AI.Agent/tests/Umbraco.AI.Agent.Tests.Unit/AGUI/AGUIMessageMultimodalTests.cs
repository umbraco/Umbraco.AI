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
        var json = """{"role":"user","content":"Hello world"}""";

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
        var json = """{"role":"user","content":null}""";

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
            "role": "user",
            "content": [
                {"type": "text", "text": "What's in this image?"},
                {"type": "binary", "mimeType": "image/png", "data": "iVBORw0KGgo=", "filename": "screenshot.png"}
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

        var binaryPart = message.ContentParts[1].ShouldBeOfType<AGUIBinaryInputContent>();
        binaryPart.MimeType.ShouldBe("image/png");
        binaryPart.Data.ShouldBe("iVBORw0KGgo=");
        binaryPart.Filename.ShouldBe("screenshot.png");
    }

    [Fact]
    public void Deserialize_ContentArray_DerivesTextFromParts()
    {
        // Arrange
        var json = """
        {
            "role": "user",
            "content": [
                {"type": "text", "text": "Hello "},
                {"type": "binary", "mimeType": "image/png", "data": "abc="},
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
            Role = AGUIMessageRole.User,
            ContentParts = new List<AGUIInputContent>
            {
                new AGUITextInputContent { Text = "Check this file" },
                new AGUIBinaryInputContent { MimeType = "application/pdf", Id = "file-123", Filename = "report.pdf" }
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
                new AGUIBinaryInputContent { MimeType = "image/jpeg", Data = "base64data", Filename = "photo.jpg" }
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

        var binaryPart = deserialized.ContentParts[1].ShouldBeOfType<AGUIBinaryInputContent>();
        binaryPart.MimeType.ShouldBe("image/jpeg");
        binaryPart.Data.ShouldBe("base64data");
        binaryPart.Filename.ShouldBe("photo.jpg");
    }

    [Fact]
    public void Deserialize_BinaryWithIdReference_PreservesId()
    {
        // Arrange — represents a message from a snapshot where base64 has been replaced with id
        var json = """
        {
            "role": "user",
            "content": [
                {"type": "text", "text": "Analyze this"},
                {"type": "binary", "mimeType": "image/png", "id": "file-abc123", "filename": "chart.png"}
            ]
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        // Assert
        var binaryPart = message!.ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        binaryPart.Id.ShouldBe("file-abc123");
        binaryPart.Data.ShouldBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_EmptyContentArray_SetsEmptyContentParts()
    {
        // Arrange
        var json = """{"role":"user","content":[]}""";

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
