// EX-2 — Connect to Cloudflare (AC7-AC9, AC27: the connection check)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

// "Test connection" goes through CloudflareExtractionCapability.GetModelsAsync, which calls the free
// model-search endpoint before returning the static model list (ARCHITECTURE.md decision 5). Unlike
// TypeSafe's paid probe, this spends no tokens. A passing check is cached for an hour per token hash.

using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareConnectionProbeTests
{
    private const string AuthenticationError =
        """{"result":null,"success":false,"errors":[{"code":10000,"message":"Authentication error"}],"messages":[]}""";

    #region Happy path

    public class GivenAValidAccountIdAndToken
    {
        private readonly ScriptedHttpMessageHandler _handler = new(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));

        private Task CheckAsync() => CloudflareTestHost.CreateCapability(_handler).GetModelsAsync(CloudflareTestHost.Settings());

        [Fact(Skip = "Pending")]
        public async Task Passes()
        {
            await Should.NotThrowAsync(CheckAsync);
        }

        [Fact(Skip = "Pending")]
        public async Task MakesExactlyOneRequest()
        {
            await CheckAsync();

            _handler.Attempts.ShouldBe(1);
        }

        [Fact(Skip = "Pending")]
        public async Task UsesGet()
        {
            await CheckAsync();

            _handler.Requests.Single().Method.ShouldBe(HttpMethod.Get);
        }

        [Fact(Skip = "Pending")]
        public async Task CallsTheModelSearchEndpointForTheAccount()
        {
            await CheckAsync();

            _handler.Requests.Single().RequestUri!.GetLeftPart(UriPartial.Path)
                .ShouldBe("https://api.cloudflare.com/client/v4/accounts/test-account/ai/models/search");
        }

        [Fact(Skip = "Pending")]
        public async Task SearchesForClairic()
        {
            await CheckAsync();

            _handler.Requests.Single().RequestUri!.Query.ShouldBe("?search=clairic");
        }

        [Fact(Skip = "Pending")]
        public async Task NeverCallsTheRunEndpoint()
        {
            // AC8: no tokens are spent, so the paid /ai/run/ endpoint must never be hit.
            await CheckAsync();

            _handler.Requests.ShouldNotContain(r => r.RequestUri!.AbsolutePath.Contains("/ai/run/"));
        }

        [Fact(Skip = "Pending")]
        public async Task SendsTheApiTokenAsABearerToken()
        {
            await CheckAsync();

            _handler.Requests.Single().Headers.Authorization!.ToString().ShouldBe("Bearer test-api-token");
        }
    }

    public class GivenASuccessfulCheckAlreadyCached
    {
        [Fact(Skip = "Pending")]
        public async Task ASecondCallWithinTheCacheWindowMakesNoSecondHttpRequest()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));
            var capability = CloudflareTestHost.CreateCapability(handler, new MemoryCache(new MemoryCacheOptions()));

            await capability.GetModelsAsync(CloudflareTestHost.Settings());
            await capability.GetModelsAsync(CloudflareTestHost.Settings());

            handler.Attempts.ShouldBe(1);
        }

        [Fact(Skip = "Pending")]
        public async Task ADifferentTokenIsCheckedAgain()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));
            var capability = CloudflareTestHost.CreateCapability(handler, new MemoryCache(new MemoryCacheOptions()));

            await capability.GetModelsAsync(CloudflareTestHost.Settings());
            await capability.GetModelsAsync(CloudflareTestHost.Settings(apiToken: "another-token"));

            handler.Attempts.ShouldBe(2);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnInvalidToken
    {
        private readonly ScriptedHttpMessageHandler _handler = new(ScriptedHttpMessageHandler.Json(AuthenticationError, HttpStatusCode.Unauthorized));

        private Task CheckAsync() => CloudflareTestHost.CreateCapability(_handler).GetModelsAsync(CloudflareTestHost.Settings());

        [Fact(Skip = "Pending")]
        public async Task Fails()
        {
            await Should.ThrowAsync<HttpRequestException>(CheckAsync);
        }

        [Fact(Skip = "Pending")]
        public async Task ReportsCloudflaresOwnErrorMessage()
        {
            var exception = await Record.ExceptionAsync(CheckAsync);

            exception!.Message.ShouldContain("Authentication error");
        }
    }

    public class GivenAFailedCheck
    {
        [Fact(Skip = "Pending")]
        public async Task IsNotCachedSoTheNextCallChecksAgain()
        {
            var handler = new ScriptedHttpMessageHandler(
                ScriptedHttpMessageHandler.Json(AuthenticationError, HttpStatusCode.Unauthorized),
                ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));
            var capability = CloudflareTestHost.CreateCapability(handler, new MemoryCache(new MemoryCacheOptions()));

            await Record.ExceptionAsync(() => capability.GetModelsAsync(CloudflareTestHost.Settings()));
            await capability.GetModelsAsync(CloudflareTestHost.Settings());

            handler.Attempts.ShouldBe(2);
        }
    }

    #endregion
}
