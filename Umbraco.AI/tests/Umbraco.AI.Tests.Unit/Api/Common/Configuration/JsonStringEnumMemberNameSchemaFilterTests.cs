using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Shouldly;
using Umbraco.AI.Web.Api.Common.Configuration;

namespace Umbraco.AI.Tests.Unit.Api.Common.Configuration;

public class JsonStringEnumMemberNameSchemaFilterTests
{
    [JsonConverter(typeof(JsonStringEnumConverter<StringEnum>))]
    private enum StringEnum
    {
        [JsonStringEnumMemberName("resolved")]
        Resolved,

        [JsonStringEnumMemberName("cancelled")]
        Cancelled,

        // No attribute — should fall back to the member name.
        Pending
    }

    private enum IntEnum
    {
        First,
        Second
    }

    [Fact]
    public void RewriteEnumValues_StringEnum_ReplacesWithWireValues()
    {
        // Arrange — mirror what Swashbuckle emits: PascalCase member names.
        var schema = new OpenApiSchema
        {
            Enum = ["Resolved", "Cancelled", "Pending"]
        };

        // Act
        JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(StringEnum));

        // Assert
        schema.Enum!.Select(node => node!.GetValue<string>())
            .ShouldBe(["resolved", "cancelled", "Pending"]);
    }

    [Fact]
    public void RewriteEnumValues_MutatesInPlace_PreservingListReference()
    {
        // Swashbuckle/Microsoft.OpenApi retain the original list reference, so the filter must
        // mutate it in place rather than reassign schema.Enum. Verify the same instance is kept.
        var schema = new OpenApiSchema
        {
            Enum = ["Resolved", "Cancelled", "Pending"]
        };
        IList<JsonNode> original = schema.Enum!;

        JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(StringEnum));

        schema.Enum.ShouldBeSameAs(original);
    }

    [Fact]
    public void RewriteEnumValues_NullableStringEnum_ReplacesWithWireValues()
    {
        var schema = new OpenApiSchema
        {
            Enum = ["Resolved", "Cancelled", "Pending"]
        };

        JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(StringEnum?));

        schema.Enum!.Select(node => node!.GetValue<string>())
            .ShouldBe(["resolved", "cancelled", "Pending"]);
    }

    [Fact]
    public void RewriteEnumValues_IntegerEnum_LeavesNumericValuesUntouched()
    {
        var schema = new OpenApiSchema
        {
            Enum = [0, 1]
        };

        JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(IntEnum));

        schema.Enum!.Select(node => node!.GetValue<int>()).ShouldBe([0, 1]);
    }

    [Fact]
    public void RewriteEnumValues_NonEnumType_IsNoOp()
    {
        var schema = new OpenApiSchema
        {
            Enum = ["a", "b"]
        };

        JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(string));

        schema.Enum!.Select(node => node!.GetValue<string>()).ShouldBe(["a", "b"]);
    }

    [Fact]
    public void RewriteEnumValues_SchemaWithoutEnum_IsNoOp()
    {
        var schema = new OpenApiSchema();

        Should.NotThrow(() => JsonStringEnumMemberNameSchemaFilter.RewriteEnumValues(schema, typeof(StringEnum)));

        schema.Enum.ShouldBeNull();
    }
}
