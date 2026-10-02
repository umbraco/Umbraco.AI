// Follow-up to DR-2 — "Test connection" must actually validate the API key. Jev has no models
// endpoint, so TypeSafeDecisionCapability.GetModelsAsync now sends one authenticated probe question
// through a real TypeSafeDecisionClient before returning the static model list; see
// TypeSafeProvider.EnsureConnectionValidAsync.
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

public class TypeSafeConnectionProbeTests
{
    private const string ProbeAnswer = """{"model":"jev-latest","answers":{"q":{"noul":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    public class GivenAnUnauthorizedApiKey
    {
        [Fact]
        public async Task GetModelsThrows()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Status(HttpStatusCode.Unauthorized));
            var capability = (IAIDecisionCapability)TypeSafeTestHost.CreateProvider(handler).GetCapability<TypeSafeDecisionCapability>();

            await Should.ThrowAsync<HttpRequestException>(
                () => capability.GetModelsAsync(new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey }));
        }
    }

    public class GivenASuccessfulProbeAlreadyCached
    {
        [Fact]
        public async Task ASecondCallWithinTheCacheWindowMakesNoSecondHttpRequest()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(ProbeAnswer));
            var provider = TypeSafeTestHost.CreateProvider(handler, new MemoryCache(new MemoryCacheOptions()));
            var capability = (IAIDecisionCapability)provider.GetCapability<TypeSafeDecisionCapability>();
            var settings = new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey };

            await capability.GetModelsAsync(settings);
            await capability.GetModelsAsync(settings);

            handler.Attempts.ShouldBe(1);
        }
    }

    public class GivenAFailedProbe
    {
        [Fact]
        public async Task IsNotCachedSoTheNextCallProbesAgain()
        {
            var handler = new ScriptedHttpMessageHandler(
                ScriptedHttpMessageHandler.Status(HttpStatusCode.Unauthorized),
                ScriptedHttpMessageHandler.Json(ProbeAnswer));
            var provider = TypeSafeTestHost.CreateProvider(handler, new MemoryCache(new MemoryCacheOptions()));
            var capability = (IAIDecisionCapability)provider.GetCapability<TypeSafeDecisionCapability>();
            var settings = new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey };

            await Record.ExceptionAsync(() => capability.GetModelsAsync(settings));
            await capability.GetModelsAsync(settings);

            handler.Attempts.ShouldBe(2);
        }
    }
}
