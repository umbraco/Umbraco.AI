using System.Text.Json;
using Shouldly;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Chat;

/// <summary>
/// Guards the polymorphic JSON contract for multimodal chat content parts.
/// The discriminator must be "$type" so it matches the published OpenAPI schema and the
/// convention used by the other polymorphic management models (e.g. profile settings);
/// a mismatch makes every <c>contentParts</c> request fail model binding with
/// "must specify a type discriminator".
/// Uses camelCase options to mirror the management API's JSON configuration.
/// </summary>
public class ChatContentPartModelTests
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Deserializes_text_part_from_dollar_type_discriminator()
    {
        const string json = """{"$type":"text","text":"hello"}""";

        var part = JsonSerializer.Deserialize<ChatContentPartModel>(json, Options);

        var text = part.ShouldBeOfType<TextChatContentPartModel>();
        text.Text.ShouldBe("hello");
    }

    [Fact]
    public void Deserializes_binary_part_from_dollar_type_discriminator()
    {
        const string json = """{"$type":"binary","mimeType":"image/png","data":"aGk="}""";

        var part = JsonSerializer.Deserialize<ChatContentPartModel>(json, Options);

        var binary = part.ShouldBeOfType<BinaryChatContentPartModel>();
        binary.MimeType.ShouldBe("image/png");
        binary.Data.ShouldBe("aGk=");
    }

    [Fact]
    public void Serializes_text_part_with_dollar_type_discriminator()
    {
        ChatContentPartModel part = new TextChatContentPartModel { Text = "hello" };

        var json = JsonSerializer.Serialize(part, Options);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("$type").GetString().ShouldBe("text");
    }

    [Fact]
    public void Round_trips_a_message_with_mixed_content_parts()
    {
        var message = new ChatMessageModel
        {
            Role = "user",
            ContentParts =
            [
                new TextChatContentPartModel { Text = "describe this" },
                new BinaryChatContentPartModel { MimeType = "image/png", Data = "aGk=" }
            ]
        };

        var json = JsonSerializer.Serialize(message, Options);
        var roundTripped = JsonSerializer.Deserialize<ChatMessageModel>(json, Options);

        roundTripped.ShouldNotBeNull();
        roundTripped!.ContentParts.ShouldNotBeNull();
        roundTripped.ContentParts!.Count.ShouldBe(2);
        var text = roundTripped.ContentParts[0].ShouldBeOfType<TextChatContentPartModel>();
        text.Text.ShouldBe("describe this");
        roundTripped.ContentParts[1].ShouldBeOfType<BinaryChatContentPartModel>();
    }
}
