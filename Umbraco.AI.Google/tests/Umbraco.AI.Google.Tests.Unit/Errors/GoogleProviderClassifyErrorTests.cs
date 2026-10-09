using Google.GenAI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Google.Tests.Unit.Fakes;

namespace Umbraco.AI.Google.Tests.Unit.Errors;

/// <remarks>
/// The messages below are verbatim from real Google API responses (free-tier Gemini key), because the
/// classifier reads them: the quota id and the API_KEY_INVALID reason only exist in the message text.
/// </remarks>
public class GoogleProviderClassifyErrorTests
{
    // 429 with the daily per-model free-tier quota exhausted.
    private const string DailyQuotaMessage =
        "You exceeded your current quota, please check your plan and billing details. For more information on this error, head to: https://ai.google.dev/gemini-api/docs/rate-limits. To monitor your current usage, head to: https://ai.dev/rate-limit. \n"
        + "* Quota exceeded for metric: generativelanguage.googleapis.com/generate_content_free_tier_requests, limit: 20, model: gemini-3.8-flash\n"
        + "Please retry in 59m32.235073147s.\n"
        + "Details: {\"@type\":\"type.googleapis.com/google.rpc.Help\",\"links\":[{\"description\":\"Learn more about Gemini API quotas\",\"url\":\"https://ai.google.dev/gemini-api/docs/rate-limits\"}]}\n"
        + "{\"@type\":\"type.googleapis.com/google.rpc.QuotaFailure\",\"violations\":[{\"quotaMetric\":\"generativelanguage.googleapis.com/generate_content_free_tier_requests\",\"quotaId\":\"GenerateRequestsPerDayPerProjectPerModel-FreeTier\",\"quotaDimensions\":{\"location\":\"global\",\"model\":\"gemini-3.8-flash\"},\"quotaValue\":\"20\"}]}\n"
        + "{\"@type\":\"type.googleapis.com/google.rpc.RetryInfo\",\"retryDelay\":\"3572s\"}";

    private const string PerMinuteQuotaMessage =
        "You exceeded your current quota. \n"
        + "Details: {\"@type\":\"type.googleapis.com/google.rpc.QuotaFailure\",\"violations\":[{\"quotaMetric\":\"generativelanguage.googleapis.com/generate_content_free_tier_input_token_count\",\"quotaId\":\"GenerateContentInputTokensPerModelPerMinute-FreeTier\",\"quotaValue\":\"250000\"}]}";

    private const string HighDemandMessage =
        "This model is currently experiencing high demand. Spikes in demand are usually temporary. Please try again later.";

    private const string InvalidKeyMessage =
        "API key not valid. Please pass a valid API key.\n"
        + "Details: {\"@type\":\"type.googleapis.com/google.rpc.ErrorInfo\",\"reason\":\"API_KEY_INVALID\",\"domain\":\"googleapis.com\",\"metadata\":{\"service\":\"generativelanguage.googleapis.com\"}}";

    private const string UnknownModelMessage =
        "models/gemini-does-not-exist is not found for API version v1beta, or is not supported for generateContent. Call ModelService.ListModels to see the list of available models and their supported methods.";

    private static AIProviderErrorInfo Classify(Exception exception)
        => new GoogleProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions()))
            .ClassifyError(exception);

    [Fact]
    public void QuotaExhausted_IsRateLimitedNotANetworkError()
    {
        // The Google SDK's errors derive from HttpRequestException but keep their status in their own
        // property, so the shared mapping alone reported this as "Couldn't reach the AI service".
        var ex = new ClientError("You exceeded your current quota.", 429, "RESOURCE_EXHAUSTED", null!);

        Classify(ex).Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    [Fact]
    public void ConnectionFailure_StillFallsBackToTheSharedNetworkMapping()
    {
        var ex = new HttpRequestException("connection refused", new System.Net.Sockets.SocketException());

        Classify(ex).Category.ShouldBe(AIProviderErrorCategory.NetworkError);
    }

    [Fact]
    public void NoGoogleApiError_IsClassifiedByTheSharedMapping()
    {
        var ex = new InvalidOperationException("nothing structured here");

        Classify(ex).ShouldBe(ProviderErrorMapping.FromException(ex));
    }

    [Fact]
    public void GoogleErrorWithoutAnHttpStatus_IsClassifiedByTheSharedMapping()
    {
        // The SDK throws without a status when it never got an HTTP response; that is not ours to classify.
        var ex = new ClientError("boom", new IOException("reset"));

        Classify(ex).ShouldBe(ProviderErrorMapping.FromException(ex));
    }

    [Fact]
    public void DailyQuotaExhausted_IsRateLimitedAndSaysTheQuotaIsDaily()
    {
        var result = Classify(new ClientError(DailyQuotaMessage, 429, "RESOURCE_EXHAUSTED", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.ProviderCode.ShouldBe("RESOURCE_EXHAUSTED");
        result.UserMessage.ShouldContain("daily");
        result.UserMessage.ShouldNotContain("wait a moment");
        result.RawMessage.ShouldContain("GenerateRequestsPerDayPerProjectPerModel-FreeTier");
    }

    [Fact]
    public void PerMinuteQuotaExhausted_KeepsTheSharedWaitAMomentMessage()
    {
        var result = Classify(new ClientError(PerMinuteQuotaMessage, 429, "RESOURCE_EXHAUSTED", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.UserMessage.ShouldBe(ProviderErrorMapping.FromHttpStatus(429, "").UserMessage);
    }

    [Fact]
    public void ModelOverloaded_IsTransient()
    {
        var result = Classify(new ServerError(HighDemandMessage, 503, "UNAVAILABLE", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.ProviderCode.ShouldBe("UNAVAILABLE");
    }

    [Fact]
    public void InvalidApiKey_IsAuthenticationNotInvalidRequest()
    {
        // Google reports a bad key as HTTP 400 INVALID_ARGUMENT, so the status alone would say "invalid request".
        var result = Classify(new ClientError(InvalidKeyMessage, 400, "INVALID_ARGUMENT", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.Authentication);
        result.ProviderCode.ShouldBe("API_KEY_INVALID");
    }

    [Fact]
    public void UnknownModel_IsNotFound()
    {
        var result = Classify(new ClientError(UnknownModelMessage, 404, "NOT_FOUND", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.NotFound);
        result.ProviderCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public void OtherBadRequest_IsInvalidRequest()
    {
        var result = Classify(new ClientError(
            "Function calling with a response mime type: 'application/json' is unsupported",
            400, "INVALID_ARGUMENT", null!));

        result.Category.ShouldBe(AIProviderErrorCategory.InvalidRequest);
    }

    [Theory]
    [InlineData(401, "UNAUTHENTICATED")]
    [InlineData(403, "PERMISSION_DENIED")]
    public void Unauthorised_IsAuthentication(int status, string googleStatus)
    {
        var result = Classify(new ClientError("denied", status, googleStatus, null!));

        result.Category.ShouldBe(AIProviderErrorCategory.Authentication);
        result.ProviderCode.ShouldBe(googleStatus);
    }

    [Theory]
    [InlineData(500, "INTERNAL")]
    [InlineData(504, "DEADLINE_EXCEEDED")]
    public void ServerFailure_IsTransient(int status, string googleStatus)
    {
        Classify(new ServerError("failed", status, googleStatus, null!)).Category
            .ShouldBe(AIProviderErrorCategory.Transient);
    }

    [Fact]
    public void GoogleErrorWrappedByAnotherException_IsStillClassified()
    {
        var inner = new ServerError(HighDemandMessage, 503, "UNAVAILABLE", null!);

        Classify(new InvalidOperationException("The chat client failed.", inner)).Category
            .ShouldBe(AIProviderErrorCategory.Transient);
    }

    [Fact]
    public void WrappedInvalidApiKey_IsStillAuthentication()
    {
        // The reason lives in the Google error's own message, not the wrapper's.
        var inner = new ClientError(InvalidKeyMessage, 400, "INVALID_ARGUMENT", null!);

        var result = Classify(new InvalidOperationException("The chat client failed.", inner));

        result.Category.ShouldBe(AIProviderErrorCategory.Authentication);
        result.ProviderCode.ShouldBe("API_KEY_INVALID");
    }

    [Fact]
    public void WrappedDailyQuota_StillSaysTheQuotaIsDaily()
    {
        var inner = new ClientError(DailyQuotaMessage, 429, "RESOURCE_EXHAUSTED", null!);

        var result = Classify(new InvalidOperationException("The chat client failed.", inner));

        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
        result.UserMessage.ShouldContain("daily");
    }
}
