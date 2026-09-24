// DR-6 — Hide disabled experimental capabilities (AC5, AC6); DR-2 AC1/AC16 at the API layer
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Management.Provider.Controllers;
using Umbraco.AI.Web.Api.Management.Provider.Mapping;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Provider;

// Real mapper + real ProviderMapDefinition + real AIExperimentalFeatures, so the spec covers
// both the new provider filter (T8) and the existing per-capability filter together.
// Assumed T8 signature: AllProviderController(AIProviderCollection, IUmbracoMapper, IAIExperimentalFeatures).
public class AllProviderControllerExperimentalTests
{
    private static IReadOnlyList<ProviderItemResponseModel> GetProviders(
        AIExperimentalOptions options,
        params IAIProvider[] providers)
    {
        var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(options);
        var experimental = new AIExperimentalFeatures(monitor.Object);

        var mapper = new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[] { new ProviderMapDefinition(experimental) }),
            Mock.Of<Umbraco.Cms.Core.Scoping.ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);

        var controller = new AllProviderController(new AIProviderCollection(() => providers), mapper, experimental);
        var result = controller.GetAllProviders().GetAwaiter().GetResult();
        return ((IEnumerable<ProviderItemResponseModel>)((OkObjectResult)result.Result!).Value!).ToList();
    }

    private static FakeAIProvider DecisionOnlyProvider()
        => new FakeAIProvider("typesafe", "TypeSafe AI").WithCapability<IAICapability>(new FakeDecisionCapability());

    private static FakeAIProvider OpenAILikeProvider()
        => new FakeAIProvider("openai", "OpenAI")
            .WithChatCapability()
            .WithEmbeddingCapability()
            .WithCapability<IAIImageGeneratorCapability>(new FakeImageGeneratorCapability());

    #region Happy path

    public class GivenTheDecisionFlagOn
    {
        private readonly IReadOnlyList<ProviderItemResponseModel> _providers =
            GetProviders(new AIExperimentalOptions { Decision = true }, DecisionOnlyProvider());

        [Fact(Skip = "Pending T8")]
        public void ListsTheDecisionOnlyProvider() => _providers.Count.ShouldBe(1);

        [Fact(Skip = "Pending T8")]
        public void ListsOnlyDecisionAsItsCapability() => _providers[0].Capabilities.ShouldBe(["Decision"]);
    }

    public class GivenAProviderWithStableAndDisabledCapabilities
    {
        private readonly IReadOnlyList<ProviderItemResponseModel> _providers =
            GetProviders(new AIExperimentalOptions(), OpenAILikeProvider());

        [Fact(Skip = "Pending T8")]
        public void StillListsTheProvider() => _providers.Count.ShouldBe(1);

        [Fact(Skip = "Pending T8")]
        public void ListsOnlyItsEnabledCapabilities()
            => _providers[0].Capabilities.ShouldBe(["Chat", "Embedding"], ignoreOrder: true);
    }

    #endregion

    #region Sad path

    public class GivenTheDecisionFlagOff
    {
        [Fact(Skip = "Pending T8")]
        public void OmitsTheDecisionOnlyProvider()
            => GetProviders(new AIExperimentalOptions(), DecisionOnlyProvider(), OpenAILikeProvider())
                .ShouldNotContain(p => p.Id == "typesafe");
    }

    #endregion
}
