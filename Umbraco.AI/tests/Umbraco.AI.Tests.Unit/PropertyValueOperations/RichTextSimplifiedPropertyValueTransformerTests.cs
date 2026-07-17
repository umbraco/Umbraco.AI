using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations.Transformers;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class RichTextSimplifiedPropertyValueTransformerTests
{
    private const string LayoutKey = "Umbraco.RichText";

    private readonly RichTextSimplifiedPropertyValueTransformer _transformer = new();

    [Fact]
    public async Task GetSimplifiedSchemaAsync_ReturnsPlainStringSchema()
    {
        var schema = await _transformer.GetSimplifiedSchemaAsync(Guid.NewGuid());

        schema.ShouldBeOfType<JsonObject>();
        schema!["type"]!.GetValue<string>().ShouldBe("string");
    }

    [Fact]
    public async Task TransformToWriteValueAsync_MarkupString_WrapsWithEmptyBlocks()
    {
        var result = await _transformer.TransformToWriteValueAsync(JsonValue.Create("<p>hi</p>"), currentValue: null, Guid.NewGuid());

        var obj = result.ShouldBeOfType<JsonObject>();
        obj["markup"]!.GetValue<string>().ShouldBe("<p>hi</p>");
        var blocks = obj["blocks"]!.AsObject();
        blocks["layout"]!.AsObject().ContainsKey(LayoutKey).ShouldBeTrue();
        blocks["layout"]![LayoutKey]!.AsArray().Count.ShouldBe(0);
        blocks["contentData"]!.AsArray().Count.ShouldBe(0);
        blocks["settingsData"]!.AsArray().Count.ShouldBe(0);
        blocks["expose"]!.AsArray().Count.ShouldBe(0);
    }

    [Fact]
    public async Task TransformToWriteValueAsync_WithCurrentBlocks_PreservesBlocksAndReplacesMarkup()
    {
        var current = new JsonObject
        {
            ["markup"] = "<p>old</p>",
            ["blocks"] = new JsonObject
            {
                ["layout"] = new JsonObject { [LayoutKey] = new JsonArray(new JsonObject { ["contentKey"] = "abc" }) },
                ["contentData"] = new JsonArray(new JsonObject { ["key"] = "abc" }),
                ["settingsData"] = new JsonArray(),
                ["expose"] = new JsonArray(),
            },
        };

        var result = await _transformer.TransformToWriteValueAsync(JsonValue.Create("<p>new</p>"), current, Guid.NewGuid());

        var obj = result.ShouldBeOfType<JsonObject>();
        obj["markup"]!.GetValue<string>().ShouldBe("<p>new</p>");
        obj["blocks"]!["contentData"]!.AsArray().Count.ShouldBe(1);
        obj["blocks"]!["layout"]![LayoutKey]!.AsArray().Count.ShouldBe(1);
    }

    [Fact]
    public async Task TransformToWriteValueAsync_AlreadyWriteShaped_PassesThrough()
    {
        var writeShaped = new JsonObject
        {
            ["markup"] = "<p>x</p>",
            ["blocks"] = new JsonObject { ["layout"] = new JsonObject { [LayoutKey] = new JsonArray() } },
        };

        var result = await _transformer.TransformToWriteValueAsync(writeShaped, currentValue: null, Guid.NewGuid());

        var obj = result.ShouldBeOfType<JsonObject>();
        obj["markup"]!.GetValue<string>().ShouldBe("<p>x</p>");
    }

    [Fact]
    public async Task TransformToWriteValueAsync_NonObjectCurrentValue_DoesNotThrowAndUsesEmptyBlocks()
    {
        // A legacy plain-string current value must not throw on `currentValue["blocks"]`.
        var result = await _transformer.TransformToWriteValueAsync(JsonValue.Create("<p>hi</p>"), JsonValue.Create("legacy string"), Guid.NewGuid());

        var obj = result.ShouldBeOfType<JsonObject>();
        obj["markup"]!.GetValue<string>().ShouldBe("<p>hi</p>");
        obj["blocks"]!["contentData"]!.AsArray().Count.ShouldBe(0);
    }

    [Fact]
    public async Task TransformToWriteValueAsync_NullValue_ProducesEmptyMarkup()
    {
        var result = await _transformer.TransformToWriteValueAsync(simplifiedValue: null, currentValue: null, Guid.NewGuid());

        var obj = result.ShouldBeOfType<JsonObject>();
        obj["markup"]!.GetValue<string>().ShouldBe(string.Empty);
    }
}
