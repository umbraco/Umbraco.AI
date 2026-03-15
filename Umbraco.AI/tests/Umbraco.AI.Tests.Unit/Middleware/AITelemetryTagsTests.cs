using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AITelemetryTagsTests
{
    [Fact]
    public void SourceName_HasExpectedValue()
    {
        AITelemetry.SourceName.ShouldBe("Umbraco.AI");
    }

    [Theory]
    [InlineData(nameof(AITelemetry.Tags.ProfileId), "umbraco.ai.profile.id")]
    [InlineData(nameof(AITelemetry.Tags.ProfileAlias), "umbraco.ai.profile.alias")]
    [InlineData(nameof(AITelemetry.Tags.EntityId), "umbraco.ai.entity.id")]
    [InlineData(nameof(AITelemetry.Tags.EntityType), "umbraco.ai.entity.type")]
    [InlineData(nameof(AITelemetry.Tags.FeatureType), "umbraco.ai.feature.type")]
    [InlineData(nameof(AITelemetry.Tags.FeatureId), "umbraco.ai.feature.id")]
    [InlineData(nameof(AITelemetry.Tags.AuditId), "umbraco.ai.audit.id")]
    [InlineData(nameof(AITelemetry.Tags.UserId), "umbraco.ai.user.id")]
    public void Tags_FollowNamingConvention(string fieldName, string expectedValue)
    {
        var value = typeof(AITelemetry.Tags)
            .GetField(fieldName)
            ?.GetValue(null) as string;

        value.ShouldBe(expectedValue);
    }

    [Fact]
    public void AllTags_UseUmbracoAiPrefix()
    {
        var fields = typeof(AITelemetry.Tags).GetFields();

        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            value.ShouldNotBeNull();
            value.ShouldStartWith("umbraco.ai.");
        }
    }
}
