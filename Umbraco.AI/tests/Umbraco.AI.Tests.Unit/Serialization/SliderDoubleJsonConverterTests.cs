using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Tests.Unit.Serialization;

public class SliderDoubleJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private sealed class Container
    {
        [JsonConverter(typeof(SliderDoubleJsonConverter))]
        public double Value { get; set; }
    }

    [Fact]
    public void Read_PlainNumber_ReturnsValue()
    {
        var result = JsonSerializer.Deserialize<Container>("""{"value":0.7}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.7);
    }

    [Fact]
    public void Read_StringNumber_ReturnsValue()
    {
        var result = JsonSerializer.Deserialize<Container>("""{"value":"0.42"}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.42);
    }

    [Fact]
    public void Read_SliderObject_ReturnsFromValue()
    {
        var result = JsonSerializer.Deserialize<Container>("""{"value":{"from":0.7,"to":0.7}}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.7);
    }

    [Fact]
    public void Read_SliderObject_RangeReturnsLowerBound()
    {
        var result = JsonSerializer.Deserialize<Container>("""{"value":{"from":0.3,"to":0.9}}""", Options);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.3);
    }

    [Fact]
    public void Read_ObjectWithoutFrom_Throws()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<Container>("""{"value":{"to":0.5}}""", Options));
    }

    [Fact]
    public void Read_BooleanValue_Throws()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<Container>("""{"value":true}""", Options));
    }

    [Fact]
    public void Write_EmitsPlainNumber()
    {
        var json = JsonSerializer.Serialize(new Container { Value = 0.7 }, Options);

        json.ShouldBe("""{"value":0.7}""");
    }
}
