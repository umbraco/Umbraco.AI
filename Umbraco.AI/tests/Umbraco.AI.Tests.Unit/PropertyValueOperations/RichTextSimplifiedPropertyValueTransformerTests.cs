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

    // Regression for issue #249: replays the exact data from a live execute request/response to prove
    // the server transform reproduces the response value, and that an empty current-blocks envelope
    // (layout: {}) is PRESERVED verbatim rather than replaced by the Empty("Umbraco.RichText") default.
    [Fact]
    public async Task TransformToWriteValueAsync_PreservesCurrentEmptyBlocksEnvelope_Issue249()
    {
        // errorMessage's current value, verbatim from the live request's serialized entity context.
        var currentValue = JsonNode.Parse(
            "{\"markup\":\"<h2>Error</h2>\\n<p>Sorry there was a problem with submitting the form. Please try again.</p>\"," +
            "\"blocks\":{\"layout\":{},\"contentData\":[],\"settingsData\":[],\"expose\":[]}}");
        var llmValue = JsonValue.Create("There was an issue with submitting the form; please try again.");

        var result = await _transformer.TransformToWriteValueAsync(llmValue, currentValue, Guid.NewGuid());

        // Byte-for-byte match with the value the live server returned for this option.
        var expected = JsonNode.Parse(
            "{\"markup\":\"There was an issue with submitting the form; please try again.\"," +
            "\"blocks\":{\"layout\":{},\"contentData\":[],\"settingsData\":[],\"expose\":[]}}")!.ToJsonString();
        result!.ToJsonString().ShouldBe(expected);

        // The preserved layout is empty ({}), NOT the no-current-value default which keys by editor alias.
        result["blocks"]!["layout"]!.AsObject().Count.ShouldBe(0);
    }
}
