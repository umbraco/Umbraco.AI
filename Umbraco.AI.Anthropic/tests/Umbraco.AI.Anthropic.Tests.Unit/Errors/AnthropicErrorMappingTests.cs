using Umbraco.AI.Anthropic.Errors;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Anthropic.Tests.Unit.Errors;

public class AnthropicErrorMappingTests
{
    [Fact]
    public void TryClassify_NoSseEnvelopeOrStatus_ReturnsNull()
    {
        // No Anthropic-specific structure → null, so the provider falls back to the shared mapping.
        var result = AnthropicErrorMapping.TryClassify(new InvalidOperationException("nothing structured here"));

        result.ShouldBeNull();
    }

    [Fact]
    public void TryClassify_OverloadedErrorSseEnvelope_MapsToTransient()
    {
        // The exact shape reported in issue #174 — Anthropic SSE error event embedded in the
        // exception message, with error.type="overloaded_error".
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"details":null,"type":"overloaded_error","message":"Overloaded"},"request_id":"req_abc123"}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = AnthropicErrorMapping.TryClassify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("overloaded_error");
        result.UserMessage.ShouldNotContain("{");  // friendly text, no raw JSON
        result.RawMessage.ShouldContain("overloaded_error");
    }

    [Fact]
    public void TryClassify_RateLimitErrorSseEnvelope_MapsToRateLimited()
    {
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"type":"rate_limit_error","message":"Rate limit exceeded"}}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = AnthropicErrorMapping.TryClassify(ex);

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
    public void TryClassify_KnownAnthropicErrorTypes_MapToExpectedCategory(string errorType, AIProviderErrorCategory expected)
    {
        var sseMessage = "SSE error returned from server: '{\"type\":\"error\",\"error\":{\"type\":\"" + errorType + "\",\"message\":\"...\"}}'";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = AnthropicErrorMapping.TryClassify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(expected);
        result.ProviderCode.ShouldBe(errorType);
    }

    [Fact]
    public void TryClassify_UnknownAnthropicErrorType_FallsBackToUnknown()
    {
        var sseMessage = """SSE error returned from server: '{"type":"error","error":{"type":"some_future_error","message":"..."}}'""";
        var ex = new global::Anthropic.Fakes.FakeAnthropicException(sseMessage);

        var result = AnthropicErrorMapping.TryClassify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        result.ProviderCode.ShouldBe("some_future_error");
    }

    [Fact]
    public void TryClassify_ExceptionWithStatusCode_UsesHttpStatusMapping()
    {
        // When no SSE envelope is present, fall through to HTTP status detection via the SDK's
        // StatusCode property.
        var ex = new global::Anthropic.Fakes.FakeAnthropicException("Server returned 429", statusCode: 429);

        var result = AnthropicErrorMapping.TryClassify(ex);

        result.ShouldNotBeNull();
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    [Fact]
    public void TryClassify_UnparseableJsonAndNoStatus_ReturnsNull()
    {
        // No usable structure → null so the provider falls back to the shared transport mapping
        // (which never leaks raw exception text to the user).
        var ex = new global::Anthropic.Fakes.FakeAnthropicException("something broke (no JSON, no status)");

        var result = AnthropicErrorMapping.TryClassify(ex);

        result.ShouldBeNull();
    }
}
