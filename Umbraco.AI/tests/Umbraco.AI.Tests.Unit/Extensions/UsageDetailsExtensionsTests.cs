using Microsoft.Extensions.AI;
using Umbraco.AI.Core;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Tests.Unit.Extensions;

/// <summary>
/// Covers how the cached-input-token count is read off <see cref="UsageDetails"/>, which is the single point
/// every provider's figure passes through on its way to the audit log and the usage analytics.
/// </summary>
public class UsageDetailsExtensionsTests
{
    [Fact]
    public void GetCachedInputTokenCount_ReadsTheFirstClassProperty()
    {
        // Arrange — where Microsoft.Extensions.AI's own adapters put it
        var usage = new UsageDetails { InputTokenCount = 100, CachedInputTokenCount = 60 };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBe(60);
    }

    [Fact]
    public void GetCachedInputTokenCount_FallsBackToTheWellKnownAdditionalCount()
    {
        // Arrange — the escape hatch for a provider whose SDK leaves the property unset
        var usage = new UsageDetails
        {
            InputTokenCount = 100,
            AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                [Constants.UsageCounts.CachedInputTokens] = 60,
            },
        };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBe(60);
    }

    [Fact]
    public void GetCachedInputTokenCount_PrefersThePropertyOverTheAdditionalCount()
    {
        // Arrange — a provider reporting both should not have the fallback win, or an out-of-date custom
        // count would quietly override the SDK's own figure
        var usage = new UsageDetails
        {
            InputTokenCount = 100,
            CachedInputTokenCount = 60,
            AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                [Constants.UsageCounts.CachedInputTokens] = 10,
            },
        };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBe(60);
    }

    [Fact]
    public void GetCachedInputTokenCount_WhenNothingWasCached_IsZeroNotNull()
    {
        // Arrange — zero is a real answer, distinct from the provider not reporting
        var usage = new UsageDetails { InputTokenCount = 100, CachedInputTokenCount = 0 };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBe(0);
    }

    [Fact]
    public void GetCachedInputTokenCount_WhenUnreported_IsNull()
    {
        // Arrange
        var usage = new UsageDetails { InputTokenCount = 100 };

        // Act & Assert — null, not zero: "not reported" must stay distinguishable from "nothing was cached"
        usage.GetCachedInputTokenCount().ShouldBeNull();
    }

    [Fact]
    public void GetCachedInputTokenCount_WhenUsageIsNull_IsNull()
        => ((UsageDetails?)null).GetCachedInputTokenCount().ShouldBeNull();

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void GetCachedInputTokenCount_WhenNegative_IsTreatedAsUnreported(long reported)
    {
        // Arrange — a negative would be a provider bug, and would corrupt every aggregate it landed in
        var usage = new UsageDetails { InputTokenCount = 100, CachedInputTokenCount = reported };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBeNull();
    }

    [Fact]
    public void GetCachedInputTokenCount_WhenTheAdditionalCountIsNegative_IsTreatedAsUnreported()
    {
        // Arrange — the fallback gets the same guard as the property
        var usage = new UsageDetails
        {
            InputTokenCount = 100,
            AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                [Constants.UsageCounts.CachedInputTokens] = -1,
            },
        };

        // Act & Assert
        usage.GetCachedInputTokenCount().ShouldBeNull();
    }
}
