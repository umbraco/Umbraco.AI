using System.Text.Json;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Tests.Unit.Tests;

public class JsonElementMergeHelperTests
{
    private static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    public class GivenAVariationConfigWithoutTheKey
    {
        private readonly JsonElement _merged = JsonElementMergeHelper.DeepMerge(
            Parse("""{"propertyAlias":"title","flag":true}"""),
            Parse("""{"other":"x"}"""));

        [Fact]
        public void ThenTheTestValueIsKept() =>
            Assert.Equal("title", _merged.GetProperty("propertyAlias").GetString());

        [Fact]
        public void ThenTheTestBooleanIsKept() =>
            Assert.True(_merged.GetProperty("flag").GetBoolean());
    }

    public class GivenAVariationConfigWithAnEmptyString
    {
        [Fact]
        public void ThenTheEmptyStringOverridesTheTestValue()
        {
            // Documents why the variation form must omit empty fields rather than send "".
            var merged = JsonElementMergeHelper.DeepMerge(
                Parse("""{"propertyAlias":"title"}"""),
                Parse("""{"propertyAlias":""}"""));

            Assert.Equal("", merged.GetProperty("propertyAlias").GetString());
        }
    }
}
