using System.Text.Json.Nodes;
using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptDynamicResponseSchemasTests
{
    private static JsonObject ColorPickerValueSchema() => new()
    {
        ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
        ["type"] = new JsonArray("object", "null"),
        ["properties"] = new JsonObject
        {
            ["value"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["label"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
        },
    };

    [Fact]
    public void BuildSingleValueSchema_WrapsPropertySchemaUnderValue()
    {
        var wrapped = AIPromptDynamicResponseSchemas.BuildSingleValueSchema(ColorPickerValueSchema());

        wrapped["type"]!.GetValue<string>().ShouldBe("object");
        wrapped["required"]!.AsArray().Select(n => n!.GetValue<string>()).ShouldBe(["value"]);
        var value = wrapped["properties"]!["value"]!.AsObject();
        value["properties"]!["label"].ShouldNotBeNull();
    }

    [Fact]
    public void BuildSingleValueSchema_StripsSchemaKeywordFromNestedPropertySchema()
    {
        var wrapped = AIPromptDynamicResponseSchemas.BuildSingleValueSchema(ColorPickerValueSchema());

        wrapped["properties"]!["value"]!.AsObject().ContainsKey("$schema").ShouldBeFalse();
    }

    [Fact]
    public void BuildSingleValueSchema_DoesNotMutateSourceSchema()
    {
        var source = ColorPickerValueSchema();

        AIPromptDynamicResponseSchemas.BuildSingleValueSchema(source);

        source.ContainsKey("$schema").ShouldBeTrue();
    }

    [Fact]
    public void BuildMultiOptionSchema_WrapsPropertySchemaUnderOptionsValue()
    {
        var wrapped = AIPromptDynamicResponseSchemas.BuildMultiOptionSchema(ColorPickerValueSchema());

        wrapped["required"]!.AsArray().Select(n => n!.GetValue<string>()).ShouldBe(["options"]);
        var optionSchema = wrapped["properties"]!["options"]!["items"]!.AsObject();
        optionSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ShouldBe(["label", "value"]);
        optionSchema["properties"]!["value"]!.AsObject().ContainsKey("$schema").ShouldBeFalse();
    }
}
