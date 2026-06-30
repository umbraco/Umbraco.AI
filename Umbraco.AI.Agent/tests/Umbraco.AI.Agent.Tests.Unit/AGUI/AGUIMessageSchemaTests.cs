using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

/// <summary>
/// Guards for #216: AGUIMessage must stay a plain POCO (no type-level converter) so OpenAPI can
/// introspect it, while the Content/ContentParts accessors keep their behavior over the backing
/// <see cref="AGUIMessageContent"/>.
/// </summary>
public class AGUIMessageSchemaTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AGUIMessage_HasNoTypeLevelConverter()
    {
        // A type-level [JsonConverter] makes the whole message (and every type only reachable through
        // it) opaque to Microsoft.AspNetCore.OpenApi. Keeping it off is the fix for #216.
        typeof(AGUIMessage).GetCustomAttribute<JsonConverterAttribute>().ShouldBeNull();
    }

    [Fact]
    public void Content_And_ContentParts_ShareBackingContent()
    {
        var message = new AGUIMessage { Id = "1", Role = AGUIMessageRole.User };

        message.Content = "hello";
        message.ContentParts = [new AGUITextInputContent { Text = "world" }];

        // Both accessors read from the same backing AGUIMessageContent.
        message.Content.ShouldBe("hello");
        message.ContentParts.ShouldNotBeNull();
        message.ContentParts.Count.ShouldBe(1);
        message.RawContent.ShouldNotBeNull();
    }

    [Fact]
    public void ClearingOneAccessor_PreservesTheOther()
    {
        var message = new AGUIMessage
        {
            Id = "1",
            Role = AGUIMessageRole.User,
            Content = "hello",
            ContentParts = [new AGUITextInputContent { Text = "world" }],
        };

        message.Content = null;

        message.Content.ShouldBeNull();
        message.ContentParts.ShouldNotBeNull(); // parts retained
        message.RawContent.ShouldNotBeNull();
    }

    [Fact]
    public void ClearingBothAccessors_NullsBackingContent()
    {
        var message = new AGUIMessage { Id = "1", Role = AGUIMessageRole.User, Content = "hello" };

        message.Content = null;

        message.RawContent.ShouldBeNull();
    }

    [Fact]
    public void StringContent_RoundTripsThroughBackingProperty()
    {
        var message = new AGUIMessage { Id = "1", Role = AGUIMessageRole.User, Content = "hi" };

        var json = JsonSerializer.Serialize(message, Options);
        var roundTripped = JsonSerializer.Deserialize<AGUIMessage>(json, Options);

        json.ShouldContain("\"content\":\"hi\"");
        roundTripped!.Content.ShouldBe("hi");
        roundTripped.ContentParts.ShouldBeNull();
    }
}
