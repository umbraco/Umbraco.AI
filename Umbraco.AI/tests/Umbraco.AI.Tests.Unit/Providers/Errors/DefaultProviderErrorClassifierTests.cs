using System.Net;
using System.Net.Http;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Tests.Unit.Providers.Errors;

public class DefaultProviderErrorClassifierTests
{
    private readonly DefaultProviderErrorClassifier _classifier = new();

    [Fact]
    public void Classify_OperationCancelled_ReturnsCancelled()
    {
        var result = _classifier.Classify(new OperationCanceledException("internal-only message"));

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Cancelled);
        result.UserMessage.ShouldNotBe(result.RawMessage);   // friendly message, not raw text
        result.RawMessage.ShouldContain("internal-only message");
    }

    [Fact]
    public void Classify_TaskCanceled_ReturnsCancelled()
    {
        // TaskCanceledException derives from OperationCanceledException — covered by the same case.
        var result = _classifier.Classify(new TaskCanceledException("token cancelled"));

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Cancelled);
    }

    [Fact]
    public void Classify_Timeout_ReturnsTransient()
    {
        var result = _classifier.Classify(new TimeoutException("upstream timeout"));

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("timeout");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AIProviderErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, AIProviderErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.NotFound, AIProviderErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, AIProviderErrorCategory.RateLimited)]
    [InlineData(HttpStatusCode.BadGateway, AIProviderErrorCategory.Transient)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AIProviderErrorCategory.Transient)]
    [InlineData(HttpStatusCode.BadRequest, AIProviderErrorCategory.InvalidRequest)]
    public void Classify_HttpRequestException_WithStatus_MapsToCategory(HttpStatusCode status, AIProviderErrorCategory expected)
    {
        var ex = new HttpRequestException("upstream failed", inner: null, status);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(expected);
    }

    [Fact]
    public void Classify_HttpRequestException_WithoutStatus_IsNetworkError()
    {
        var ex = new HttpRequestException("connection refused");

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.NetworkError);
    }

    [Fact]
    public void Classify_WrappedHttpRequestException_WalksInnerChain()
    {
        // The OpenAI SDK and others wrap HttpRequestException in transport-layer exceptions.
        var inner = new HttpRequestException("network", inner: null, HttpStatusCode.ServiceUnavailable);
        var outer = new InvalidOperationException("wrapped", inner);

        var result = _classifier.Classify(outer);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
    }

    [Fact]
    public void Classify_UnknownException_ReturnsNull()
    {
        // Anything outside the BCL types the default classifier handles falls through.
        var result = _classifier.Classify(new InvalidOperationException("not a recognised type"));

        result.ShouldBeNull();
    }
}
