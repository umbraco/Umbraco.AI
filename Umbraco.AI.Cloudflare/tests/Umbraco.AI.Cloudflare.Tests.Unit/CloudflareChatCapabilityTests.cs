// Chat capability: the model list comes from Cloudflare's model search (its OpenAI-compatible API has no /models
// route) and chat itself goes through the OpenAI client pointed at {Endpoint}/accounts/{AccountId}/ai/v1.

using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareChatCapabilityTests
{
    private const string TwoModels =
        """{"result":[{"name":"@cf/meta/llama-3.3-70b-instruct-fp8-fast","task":{"name":"Text Generation"}},{"name":"@hf/thebloke/zephyr-7b-beta-awq","task":{"name":"Text Generation"}}],"result_info":{"page":1,"per_page":100,"total_pages":1},"success":true,"errors":[],"messages":[]}""";

    private const string AuthenticationError =
        """{"result":null,"success":false,"errors":[{"code":10000,"message":"Authentication error"}],"messages":[]}""";

    #region Happy path

    public class GivenAValidConnection
    {
        private readonly ScriptedHttpMessageHandler _handler = new(ScriptedHttpMessageHandler.Json(TwoModels));

        private Task<IReadOnlyList<Umbraco.AI.Core.Models.AIModelDescriptor>> ListAsync()
            => CloudflareTestHost.CreateChatCapability(_handler).GetModelsAsync(CloudflareTestHost.Settings());

        [Fact]
        public async Task ListsTheModelsFromTheSearch()
        {
            var models = await ListAsync();

            models.Select(m => m.Model.ModelId).ShouldBe(
                ["@cf/meta/llama-3.3-70b-instruct-fp8-fast", "@hf/thebloke/zephyr-7b-beta-awq"]);
        }

        [Fact]
        public async Task TagsEachModelWithTheCloudflareProvider()
        {
            var models = await ListAsync();

            models.ShouldAllBe(m => m.Model.ProviderId == "cloudflare");
        }

        [Fact]
        public async Task DropsTheCfPrefixFromTheDisplayName()
        {
            var models = await ListAsync();

            models.Select(m => m.Name).ShouldBe(
                ["meta/llama-3.3-70b-instruct-fp8-fast", "@hf/thebloke/zephyr-7b-beta-awq"]);
        }

        [Fact]
        public async Task SearchesTheAccountsTextGenerationModels()
        {
            await ListAsync();

            var uri = _handler.Requests.Single().RequestUri!;
            uri.GetLeftPart(UriPartial.Path).ShouldBe("https://api.cloudflare.com/client/v4/accounts/test-account/ai/models/search");
            uri.Query.ShouldContain("task=Text%20Generation");
        }

        [Fact]
        public async Task AuthenticatesWithTheTokenAsABearerHeader()
        {
            await ListAsync();

            var header = _handler.Requests.Single().Headers.Authorization!;
            header.Scheme.ShouldBe("Bearer");
            header.Parameter.ShouldBe(CloudflareTestHost.ApiToken);
        }

        [Fact]
        public async Task CachesTheListSoASecondCallSendsNothing()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var capability = CloudflareTestHost.CreateChatCapability(_handler, cache);

            await capability.GetModelsAsync(CloudflareTestHost.Settings());
            await capability.GetModelsAsync(CloudflareTestHost.Settings());

            _handler.Attempts.ShouldBe(1);
        }
    }

    public class GivenMoreModelsThanOnePage
    {
        [Fact]
        public async Task FollowsThePaginationAndMergesThePages()
        {
            var firstPage = "{\"result\":[" + string.Join(",", Enumerable.Range(0, 100).Select(i => $"{{\"name\":\"@cf/a/model-{i:000}\"}}")) +
                            "],\"result_info\":{\"page\":1,\"per_page\":100,\"total_pages\":2},\"success\":true}";
            var secondPage = """{"result":[{"name":"@cf/z/last-model"}],"result_info":{"page":2,"per_page":100,"total_pages":2},"success":true}""";
            var handler = new ScriptedHttpMessageHandler(
                ScriptedHttpMessageHandler.Json(firstPage),
                ScriptedHttpMessageHandler.Json(secondPage));

            var models = await CloudflareTestHost.CreateChatCapability(handler).GetModelsAsync(CloudflareTestHost.Settings());

            models.Count.ShouldBe(101);
            handler.Attempts.ShouldBe(2);
            handler.Requests[1].RequestUri!.Query.ShouldContain("page=2");
        }
    }

    #endregion

    #region Failure

    public class GivenARejectedToken
    {
        [Fact]
        public async Task ThrowsWithCloudflaresOwnMessage()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(AuthenticationError, HttpStatusCode.Forbidden));

            var ex = await Should.ThrowAsync<HttpRequestException>(
                () => CloudflareTestHost.CreateChatCapability(handler).GetModelsAsync(CloudflareTestHost.Settings()));

            ex.Message.ShouldContain("Authentication error");
        }

        [Fact]
        public async Task NeverPutsTheTokenInTheMessage()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(AuthenticationError, HttpStatusCode.Forbidden));

            var ex = await Should.ThrowAsync<HttpRequestException>(
                () => CloudflareTestHost.CreateChatCapability(handler).GetModelsAsync(CloudflareTestHost.Settings()));

            ex.Message.ShouldNotContain(CloudflareTestHost.ApiToken);
        }

        [Fact]
        public async Task DoesNotCacheTheFailure()
        {
            var handler = new ScriptedHttpMessageHandler(
                ScriptedHttpMessageHandler.Json(AuthenticationError, HttpStatusCode.Forbidden),
                ScriptedHttpMessageHandler.Json(TwoModels));
            var capability = CloudflareTestHost.CreateChatCapability(handler);

            await Should.ThrowAsync<HttpRequestException>(() => capability.GetModelsAsync(CloudflareTestHost.Settings()));
            var models = await capability.GetModelsAsync(CloudflareTestHost.Settings());

            models.Count.ShouldBe(2);
        }
    }

    public class GivenMissingSettings
    {
        [Fact]
        public async Task RejectsAMissingTokenBeforeAnyRequest()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(TwoModels));

            await Should.ThrowAsync<InvalidOperationException>(
                () => CloudflareTestHost.CreateChatCapability(handler).GetModelsAsync(CloudflareTestHost.Settings(apiToken: null)));

            handler.Attempts.ShouldBe(0);
        }
    }

    #endregion
}
