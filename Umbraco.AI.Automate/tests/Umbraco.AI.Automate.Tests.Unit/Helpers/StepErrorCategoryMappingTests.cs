using Shouldly;
using Umbraco.AI.Automate.Helpers;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.Automate.Core.Actions;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Helpers;

public class StepErrorCategoryMappingTests
{
    private static AIProviderException ProviderError(AIProviderErrorCategory category)
        => new(new AIProviderErrorInfo(category, "user-safe message", ProviderCode: null, "raw message"), new Exception("sdk"));

    [Theory]
    [InlineData(AIProviderErrorCategory.RateLimited, StepRunErrorCategory.RateLimiting)]
    [InlineData(AIProviderErrorCategory.Transient, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(AIProviderErrorCategory.NetworkError, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(AIProviderErrorCategory.Authentication, StepRunErrorCategory.Authentication)]
    [InlineData(AIProviderErrorCategory.InvalidRequest, StepRunErrorCategory.ConfigurationError)]
    [InlineData(AIProviderErrorCategory.NotFound, StepRunErrorCategory.ConfigurationError)]
    [InlineData(AIProviderErrorCategory.Cancelled, StepRunErrorCategory.Cancelled)]
    [InlineData(AIProviderErrorCategory.Unknown, StepRunErrorCategory.Unknown)]
    public void FromException_ProviderError_MapsToTheMatchingStepCategory(
        AIProviderErrorCategory providerCategory,
        StepRunErrorCategory expected)
    {
        StepErrorCategoryMapping.FromException(ProviderError(providerCategory)).ShouldBe(expected);
    }

    [Fact]
    public void FromException_ProviderErrorWrappedByAnotherException_IsStillMapped()
    {
        var wrapped = new InvalidOperationException("agent failed", ProviderError(AIProviderErrorCategory.RateLimited));

        StepErrorCategoryMapping.FromException(wrapped).ShouldBe(StepRunErrorCategory.RateLimiting);
    }

    [Fact]
    public void FromException_NotAProviderFailure_IsUnknown()
    {
        StepErrorCategoryMapping.FromException(new InvalidCastException("not ours")).ShouldBe(StepRunErrorCategory.Unknown);
    }

    // Automate retries every category except these (see DefaultStepErrorClassifier), so the mapping decides
    // whether a rejected key or an unknown model burns the retry budget, and a quota error still gets one.
    [Theory]
    [InlineData(AIProviderErrorCategory.Authentication, true)]
    [InlineData(AIProviderErrorCategory.NotFound, true)]
    [InlineData(AIProviderErrorCategory.InvalidRequest, true)]
    [InlineData(AIProviderErrorCategory.RateLimited, false)]
    [InlineData(AIProviderErrorCategory.Transient, false)]
    public void FromException_FailuresThatCannotSucceedOnRetry_AreTheOnesAutomateTreatsAsTerminal(
        AIProviderErrorCategory providerCategory,
        bool expectTerminal)
    {
        var category = StepErrorCategoryMapping.FromException(ProviderError(providerCategory));

        var terminal = category is StepRunErrorCategory.Validation
            or StepRunErrorCategory.ConfigurationError
            or StepRunErrorCategory.Authentication
            or StepRunErrorCategory.Cancelled;
        terminal.ShouldBe(expectTerminal);
    }
}
