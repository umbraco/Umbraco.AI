using Umbraco.AI.Anthropic.Errors;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Anthropic.Tests.Unit.Errors;

public class AnthropicProviderErrorClassifierTests
{
    private readonly AnthropicProviderErrorClassifier _classifier = new();

    [Fact]
    public void Classify_NonAnthropicException_ReturnsNull()
    {
        // Anything outside the Anthropic.* namespace should fall through to other classifiers.
        var result = _classifier.Classify(new InvalidOperationException("not from Anthropic"));

        result.ShouldBeNull();
    }

    [Fact]
    public void Classify_OverloadedErrorSseEnvelope_MapsToTransient()
    {
        // The exact shape reported in issue #174 — Anthropic SSE error event embedded in the
        // exception message, with error.type="overloaded_error".
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"details":null,"type":"overloaded_error","message":"Overloaded"},"request_id":"req_abc123"}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("overloaded_error");
        result.UserMessage.ShouldNotContain("{");  // friendly text, no raw JSON
        result.RawMessage.ShouldContain("overloaded_error");
    }

    [Fact]
    public void Classify_RateLimitErrorSseEnvelope_MapsToRateLimited()
    {
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"type":"rate_limit_error","message":"Rate limit exceeded"}}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.ProviderCode.ShouldBe("rate_limit_error");
    }

    [Theory]
    [InlineData("authentication_error", AIProviderErrorCategory.Authentication)]
    [InlineData("permission_error", AIProviderErrorCategory.Authentication)]
    [InlineData("not_found_error", AIProviderErrorCategory.NotFound)]
    [InlineData("invalid_request_error", AIProviderErrorCategory.InvalidRequest)]
    [InlineData("api_error", AIProviderErrorCategory.Transient)]
    public void Classify_KnownAnthropicErrorTypes_MapToExpectedCategory(string errorType, AIProviderErrorCategory expected)
    {
        var sseMessage = "SSE error returned from server: '{\"type\":\"error\",\"error\":{\"type\":\"" + errorType + "\",\"message\":\"...\"}}'";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(expected);
        result.ProviderCode.ShouldBe(errorType);
    }

    [Fact]
    public void Classify_UnknownAnthropicErrorType_FallsBackToUnknown()
    {
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"type":"some_future_error","message":"..."}}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        result.ProviderCode.ShouldBe("some_future_error");
    }

    [Fact]
    public void Classify_AnthropicExceptionWithStatusCode_UsesHttpStatusMapping()
    {
        // When no SSE envelope is present, the classifier falls through to HTTP status detection
        // via the SDK's StatusCode property.
        var ex = new global::Anthropic.Fakes.FakeAnthropicException("Server returned 429", statusCode: 429);

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    [Fact]
    public void Classify_AnthropicExceptionWithUnparseableJson_ReturnsUnknown()
    {
        // Recognised as Anthropic but no usable structure → friendly fallback, not raw text.
        var ex = new global::Anthropic.Fakes.FakeAnthropicException("something broke (no JSON, no status)");

        var result = _classifier.Classify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        result.UserMessage.ShouldNotContain("something broke");
    }
}
