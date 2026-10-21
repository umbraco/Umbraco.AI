// EX-2 — Connect to Cloudflare (T4 review: the token and account ID are handled safely)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareSecurityTests
{
    private const string TextAnswer =
        """{"result":{"answers":{"orderId":{"value":"A-88412"}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private static readonly AITextExtractionField OrderId = new() { Id = "orderId", Instructions = "The order number" };

    private static CloudflareProviderSettings SettingsWithAccountId(string accountId)
        => new() { AccountId = accountId, ApiToken = CloudflareTestHost.ApiToken };

    // A body that echoes the token back, in both the structured and the unstructured form.
    private static readonly string[] TokenEchoingBodies =
    [
        $$"""{"success":false,"errors":[{"code":10000,"message":"Bad token {{CloudflareTestHost.ApiToken}}"}]}""",
        $"Unauthorized: {CloudflareTestHost.ApiToken}",
    ];

    public class GivenAnAccountIdContainingASlash
    {
        [Fact]
        public async Task EscapesItInTheRunUrl()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(TextAnswer));
            var capability = CloudflareTestHost.CreateCapability(handler);
            using var client = await capability.CreateClientAsync(SettingsWithAccountId("acc/../other"), null, default);

            await client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412.", Fields = [OrderId] });

            handler.Requests.Single().RequestUri!.AbsoluteUri.ShouldBe(
                "https://api.cloudflare.com/client/v4/accounts/acc%2F..%2Fother/ai/run/@cf/cloudflare/clairic");
        }

        [Fact]
        public async Task EscapesItInTheConnectionCheckUrl()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));

            await CloudflareTestHost.CreateCapability(handler).GetModelsAsync(SettingsWithAccountId("acc/../other"));

            handler.Requests.Single().RequestUri!.AbsoluteUri.ShouldBe(
                "https://api.cloudflare.com/client/v4/accounts/acc%2F..%2Fother/ai/models/search?search=clairic");
        }
    }

    public class GivenAFailedExtractionWhoseBodyEchoesTheToken
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task TheExceptionTextDoesNotContainTheToken(int bodyIndex)
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(TokenEchoingBodies[bodyIndex], HttpStatusCode.Unauthorized));
            var client = await CloudflareTestHost.CreateClientAsync(handler);

            var exception = await Record.ExceptionAsync(
                () => client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412.", Fields = [OrderId] }));

            exception!.ToString().ShouldNotContain(CloudflareTestHost.ApiToken);
        }
    }

    public class GivenAFailedExtractionWhoseBodyHasTheTokenAcrossTheTruncationPoint
    {
        private static string Filler => new('x', 490);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NoPartOfTheTokenAppearsInTheExceptionText(bool structured)
        {
            var body = structured
                ? $$"""{"success":false,"errors":[{"code":10000,"message":"{{Filler}}{{CloudflareTestHost.ApiToken}} tail"}]}"""
                : $"{Filler}{CloudflareTestHost.ApiToken} tail";
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(body, HttpStatusCode.Unauthorized));
            var client = await CloudflareTestHost.CreateClientAsync(handler);

            var exception = await Record.ExceptionAsync(
                () => client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412.", Fields = [OrderId] }));

            exception!.Message.ShouldNotContain(CloudflareTestHost.ApiToken[..8]);
        }
    }

    public class GivenASuccessfulResponseThatReportsFailure
    {
        private const string FailureBody =
            """{"success":false,"errors":[{"code":10001,"message":"Quota exceeded for test-api-token"}]}""";

        private static async Task<Exception> ExtractAsync()
        {
            var client = await CloudflareTestHost.CreateClientAsync(new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(FailureBody)));
            return (await Record.ExceptionAsync(
                () => client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412.", Fields = [OrderId] })))!;
        }

        [Fact]
        public async Task TheMessageNamesTheErrorCode() => (await ExtractAsync()).Message.ShouldContain("10001");

        [Fact]
        public async Task TheMessageNamesTheErrorMessage() => (await ExtractAsync()).Message.ShouldContain("Quota exceeded");

        [Fact]
        public async Task TheMessageDoesNotContainTheToken() => (await ExtractAsync()).Message.ShouldNotContain(CloudflareTestHost.ApiToken);

        [Fact]
        public async Task IsNotClassifiedAsANetworkError()
            => ProviderErrorMapping.FromException(await ExtractAsync()).Category.ShouldNotBe(AIProviderErrorCategory.NetworkError);
    }

    public class GivenAFailedConnectionCheckWhoseBodyEchoesTheToken
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task TheExceptionTextDoesNotContainTheToken(int bodyIndex)
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(TokenEchoingBodies[bodyIndex], HttpStatusCode.Unauthorized));

            var exception = await Record.ExceptionAsync(
                () => CloudflareTestHost.CreateCapability(handler).GetModelsAsync(CloudflareTestHost.Settings()));

            exception!.ToString().ShouldNotContain(CloudflareTestHost.ApiToken);
        }
    }

    public class GivenASuccessfulConnectionCheck
    {
        [Fact]
        public async Task TheCacheKeyDoesNotContainTheRawToken()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));

            await CloudflareTestHost.CreateCapability(handler, cache).GetModelsAsync(CloudflareTestHost.Settings());

            cache.Keys.Cast<string>().Single().ShouldNotContain(CloudflareTestHost.ApiToken);
        }
    }

    public class GivenATokenWithAControlCharacter
    {
        [Fact]
        public async Task CreatingAClientFailsWithoutQuotingTheToken()
        {
            var capability = CloudflareTestHost.CreateCapability();

            var exception = await Record.ExceptionAsync(
                () => capability.CreateClientAsync(CloudflareTestHost.Settings("secret\r\nX-Injected: 1"), null, default));

            exception!.Message.ShouldNotContain("secret");
        }
    }
}
