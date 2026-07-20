using System.Text.Json.Nodes;
using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptSchemaCompatibilityTests
{
    [Fact]
    public void IsStrictRepresentable_PlainStringSchema_ReturnsTrue()
    {
        var schema = new JsonObject { ["type"] = "string" };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeTrue();
    }

    [Fact]
    public void IsStrictRepresentable_WellTypedObjectSchema_ReturnsTrue()
    {
        // ColorPicker-style { value, label } — the schema the schema-driven wand is meant to keep.
        var schema = new JsonObject
        {
            ["type"] = new JsonArray("object", "null"),
            ["properties"] = new JsonObject
            {
                ["value"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                ["label"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            },
        };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeTrue();
    }

    [Fact]
    public void IsStrictRepresentable_TypedArraySchema_ReturnsTrue()
    {
        var schema = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonObject { ["type"] = "string" },
        };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeTrue();
    }

    [Fact]
    public void IsStrictRepresentable_EmptySchema_ReturnsFalse()
    {
        // The block editor "any type" node: {} with no `type`.
        AIPromptSchemaCompatibility.IsStrictRepresentable(new JsonObject()).ShouldBeFalse();
    }

    [Fact]
    public void IsStrictRepresentable_ArrayWithoutItems_ReturnsFalse()
    {
        var schema = new JsonObject { ["type"] = "array" };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeFalse();
    }

    [Fact]
    public void IsStrictRepresentable_NestedUnconstrainedNode_ReturnsFalse()
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["alias"] = new JsonObject { ["type"] = "string" },
                ["value"] = new JsonObject(), // unconstrained
            },
        };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeFalse();
    }

    [Fact]
    public void IsStrictRepresentable_EnumWithoutType_ReturnsTrue()
    {
        var schema = new JsonObject { ["enum"] = new JsonArray("a", "b") };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeTrue();
    }

    [Fact]
    public void IsStrictRepresentable_AnyOfWithRepresentableBranches_ReturnsTrue()
    {
        var schema = new JsonObject
        {
            ["anyOf"] = new JsonArray(
                new JsonObject { ["type"] = "string" },
                new JsonObject { ["type"] = "number" }),
        };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeTrue();
    }

    [Fact]
    public void IsStrictRepresentable_AnyOfWithUnconstrainedBranch_ReturnsFalse()
    {
        var schema = new JsonObject
        {
            ["anyOf"] = new JsonArray(
                new JsonObject { ["type"] = "string" },
                new JsonObject()),
        };

        AIPromptSchemaCompatibility.IsStrictRepresentable(schema).ShouldBeFalse();
    }

    [Fact]
    public void IsStrictRepresentable_BlockListValueSchema_ReturnsFalse()
    {
        // Mirrors Umbraco CMS BlockJsonSchemaHelper output: the values[].value node is `{}`.
        AIPromptSchemaCompatibility.IsStrictRepresentable(BlockListValueSchema()).ShouldBeFalse();
    }

    [Fact]
    public void IsStrictRepresentable_Null_ReturnsFalse()
    {
        AIPromptSchemaCompatibility.IsStrictRepresentable(null).ShouldBeFalse();
    }

    private static JsonObject BlockListValueSchema() => new()
    {
        ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
        ["type"] = new JsonArray("object", "null"),
        ["properties"] = new JsonObject
        {
            ["contentData"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["required"] = new JsonArray("key", "contentTypeKey"),
                    ["properties"] = new JsonObject
                    {
                        ["key"] = new JsonObject { ["type"] = "string" },
                        ["contentTypeKey"] = new JsonObject { ["type"] = "string" },
                        ["values"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject
                                {
                                    ["alias"] = new JsonObject { ["type"] = "string" },
                                    ["culture"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                                    ["segment"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                                    ["value"] = new JsonObject(), // Any type - depends on property editor
                                },
                            },
                        },
                    },
                },
            },
        },
    };
}
