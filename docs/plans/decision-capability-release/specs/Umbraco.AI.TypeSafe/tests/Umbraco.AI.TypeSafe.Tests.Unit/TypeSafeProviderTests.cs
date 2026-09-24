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

        [Fact(Skip = "Pending T6")]
        public void HasTheTypeSafeProviderId()
        {
            _provider.Id.ShouldBe("typesafe");
        }

        [Fact(Skip = "Pending T6")]
        public void ExposesOnlyTheDecisionCapability()
        {
            _provider.GetCapabilities().Select(c => c.Kind).ShouldBe([AICapability.Decision]);
        }

        [Fact(Skip = "Pending T6")]
        public async Task ListsJevLatestAsItsOnlyModel()
        {
            var capability = (IAIDecisionCapability)_provider.GetCapability<TypeSafeDecisionCapability>();

            var models = await capability.GetModelsAsync(new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey });

            models.Select(m => m.Model.ModelId).ShouldBe(["jev-latest"]);
        }
    }

    public class GivenSettingsWithNoEndpoint
    {
        [Fact(Skip = "Pending T6")]
        public void DefaultsTheEndpointToTheTypeSafeApi()
        {
            new TypeSafeProviderSettings().Endpoint.ShouldBe("https://api.typesafe.ai");
        }
    }

    public class GivenSettingsWithNoApiKey
    {
        private readonly IAIDecisionCapability _capability =
            (IAIDecisionCapability)TypeSafeTestHost.CreateProvider().GetCapability<TypeSafeDecisionCapability>();

        [Fact(Skip = "Pending T6")]
        public async Task CreatingAClientFailsBeforeAnyNetworkCall()
        {
            await Should.ThrowAsync<InvalidOperationException>(
                () => _capability.CreateClientAsync(new TypeSafeProviderSettings { ApiKey = null }, null, default));
        }
    }
}
