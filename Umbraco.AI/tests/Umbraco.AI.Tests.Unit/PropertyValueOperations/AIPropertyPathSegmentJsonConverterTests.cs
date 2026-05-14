using System.Text.Json;
using Umbraco.AI.Core.PropertyValueOperations;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class AIPropertyPathSegmentJsonConverterTests
{
    [Fact]
    public void Read_StringToken_ProducesPropertyAliasSegment()
    {
        var segment = JsonSerializer.Deserialize<AIPropertyPathSegment>("\"contentBlocks\"");

        segment.ShouldBeOfType<AIPropertyPathSegment.PropertyAliasSegment>();
        ((AIPropertyPathSegment.PropertyAliasSegment)segment!).Alias.ShouldBe("contentBlocks");
    }

    [Fact]
    public void Read_ObjectWithBlockKey_ProducesBlockKeySegment()
    {
        var key = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var json = $"{{ \"blockKey\": \"{key}\" }}";

        var segment = JsonSerializer.Deserialize<AIPropertyPathSegment>(json);

        segment.ShouldBeOfType<AIPropertyPathSegment.BlockKeySegment>();
        ((AIPropertyPathSegment.BlockKeySegment)segment!).BlockKey.ShouldBe(key);
    }

    [Fact]
    public void Read_ObjectMissingBlockKey_Throws()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<AIPropertyPathSegment>("{ \"foo\": \"bar\" }"));
    }

    [Fact]
    public void Write_PropertyAliasSegment_EmitsBareString()
    {
        var segment = AIPropertyPathSegment.ForProperty("title");

        var json = JsonSerializer.Serialize(segment);

        json.ShouldBe("\"title\"");
    }

    [Fact]
    public void Write_BlockKeySegment_EmitsObjectWithBlockKey()
    {
        var key = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var segment = AIPropertyPathSegment.ForBlock(key);

        var json = JsonSerializer.Serialize(segment);

        json.ShouldBe($"{{\"blockKey\":\"{key}\"}}");
    }

    [Fact]
    public void Read_FullPathArray_RoundTrips()
    {
        var key = Guid.NewGuid();
        var json = $"[\"contentBlocks\", {{\"blockKey\":\"{key}\"}}, \"innerBlocks\"]";

        var segments = JsonSerializer.Deserialize<AIPropertyPathSegment[]>(json);

        segments.ShouldNotBeNull();
        segments!.Length.ShouldBe(3);
        ((AIPropertyPathSegment.PropertyAliasSegment)segments[0]).Alias.ShouldBe("contentBlocks");
        ((AIPropertyPathSegment.BlockKeySegment)segments[1]).BlockKey.ShouldBe(key);
        ((AIPropertyPathSegment.PropertyAliasSegment)segments[2]).Alias.ShouldBe("innerBlocks");
    }
}
