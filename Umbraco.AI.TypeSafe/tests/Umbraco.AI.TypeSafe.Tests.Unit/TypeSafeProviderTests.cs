// DR-2 — Connect to TypeSafe AI (AC1; AC16's provider-listing half lives in Web's provider-listing specs, T8)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

public class TypeSafeProviderTests
{
    public class GivenTheProvider
    {
        private readonly TypeSafeProvider _provider = TypeSafeTestHost.CreateProvider();

        [Fact]
        public void HasTheTypeSafeProviderId()
        {
            _provider.Id.ShouldBe("typesafe");
        }

        [Fact]
        public void ExposesOnlyTheDecisionCapability()
        {
            _provider.GetCapabilities().Select(c => c.Kind).ShouldBe([AICapability.Decision]);
        }
    }

    public class GivenAValidApiKey
    {
        private const string ProbeAnswer = """{"model":"jev-latest","answers":{"q":{"noul":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";

        [Fact]
        public async Task ListsJevLatestAsItsOnlyModel()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(ProbeAnswer));
            var capability = (IAIDecisionCapability)TypeSafeTestHost.CreateProvider(handler).GetCapability<TypeSafeDecisionCapability>();

            var models = await capability.GetModelsAsync(new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey });

            models.Select(m => m.Model.ModelId).ShouldBe(["jev-latest"]);
        }
    }

    public class GivenSettingsWithNoEndpoint
    {
        [Fact]
        public void DefaultsTheEndpointToTheTypeSafeApi()
        {
            new TypeSafeProviderSettings().Endpoint.ShouldBe("https://api.typesafe.ai");
        }
    }

    public class GivenSettingsWithNoApiKey
    {
        private readonly ScriptedHttpMessageHandler _handler = new(ScriptedHttpMessageHandler.Status(System.Net.HttpStatusCode.OK));
        private readonly IAIDecisionCapability _capability;

        public GivenSettingsWithNoApiKey()
            => _capability = (IAIDecisionCapability)TypeSafeTestHost.CreateProvider(_handler).GetCapability<TypeSafeDecisionCapability>();

        [Fact]
        public async Task CreatingAClientFailsBeforeAnyNetworkCall()
        {
            await Should.ThrowAsync<InvalidOperationException>(
                () => _capability.CreateClientAsync(new TypeSafeProviderSettings { ApiKey = null }, null, default));
        }

        [Fact]
        public async Task NeverReachesTheHandler()
        {
            await Record.ExceptionAsync(
                () => _capability.CreateClientAsync(new TypeSafeProviderSettings { ApiKey = null }, null, default));

            _handler.Requests.ShouldBeEmpty();
        }
    }
}
