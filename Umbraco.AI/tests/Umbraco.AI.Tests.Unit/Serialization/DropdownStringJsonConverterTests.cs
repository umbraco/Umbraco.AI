using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Tests.Unit.Serialization;

public class DropdownStringJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private sealed class Container
    {
        [JsonConverter(typeof(DropdownStringJsonConverter))]
        public string? Value { get; set; }
    }

    [Fact]
    public void Read_SingleElementArray_ReturnsTheElement()
    {
        // The shape the backoffice dropdown actually saves, even as a single select.
        var result = JsonSerializer.Deserialize<Container>("""{"value":["low"]}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("low");
    }

    [Fact]
    public void Read_PlainString_ReturnsValue()
    {
        // What an API caller or a normalised store sends.
        var result = JsonSerializer.Deserialize<Container>("""{"value":"high"}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("high");
    }

    [Fact]
    public void Read_EmptyArray_ReturnsNull()
    {
        // The dropdown's cleared state.
        var result = JsonSerializer.Deserialize<Container>("""{"value":[]}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBeNull();
    }

    [Fact]
    public void Read_Null_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<Container>("""{"value":null}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBeNull();
    }

    [Fact]
    public void Read_MultipleElements_ReturnsTheFirst()
    {
        // A multi-select bound to a string property is a declaration mistake; it should not fail a request.
        var result = JsonSerializer.Deserialize<Container>("""{"value":["low","high"]}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("low");
    }

    [Fact]
    public void Read_NonStringArrayElement_Throws()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Container>("""{"value":[7]}""", Options));
    }

    [Fact]
    public void Read_UnexpectedToken_Throws()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Container>("""{"value":7}""", Options));
    }

    [Fact]
    public void Write_RoundTripsAsAScalar()
    {
        // Writing a scalar is what normalises the stored shape on the next save.
        var json = JsonSerializer.Serialize(new Container { Value = "medium" }, Options);

        json.ShouldBe("""{"value":"medium"}""");
    }

    [Fact]
    public void Write_Null_WritesNull()
    {
        var json = JsonSerializer.Serialize(new Container { Value = null }, Options);

        json.ShouldBe("""{"value":null}""");
    }
}
