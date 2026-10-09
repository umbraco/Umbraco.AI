#if MODEL_FACTS_PENDING // Pending: T4 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-3: Core turns standard Metadata into facts (AC1-AC9).
// AIMetadataModelFactProvider is assumed to have a parameterless constructor.
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;

namespace Umbraco.AI.Tests.Unit.ModelFacts;

public class AIMetadataModelFactProviderTests
{
    private const string ModelId = "anthropic/claude-sonnet-4";

    private static readonly AIModelFactContext Context = new()
    {
        ConnectionId = Guid.NewGuid(),
        ProviderId = "openrouter",
        Capability = AICapability.Chat,
    };

    private static AIModelDescriptor Descriptor(IReadOnlyDictionary<string, string> metadata)
        => new(new AIModelRef("openrouter", ModelId), "Claude Sonnet 4", metadata);

    private static IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> GetFacts(AIModelDescriptor descriptor)
        => new AIMetadataModelFactProvider()
            .GetModelFactsAsync(Context, [descriptor], CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    private static IEnumerable<string> FactKeys(IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> facts)
        => facts.TryGetValue(ModelId, out var modelFacts) ? modelFacts.Select(f => f.Key) : [];

    // Throws (failing the spec) when the fact is missing, so a value assertion can never pass vacuously.
    private static AIModelFact FactWithKey(
        IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> facts,
        string key)
        => facts[ModelId].Single(f => f.Key == key);

    // ---------------------------------------------------------------- Happy path

    public class GivenAContextWindowOf200000
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> _facts =
            GetFacts(Descriptor(AIModelMetadata.ForContextWindow(200000)));

        [Fact] // MF-3 AC1
        public void TheModelHasACoreContextWindowFact()
            => FactKeys(_facts).ShouldContain("core.contextWindow");

        [Fact] // MF-3 AC1
        public void TheContextWindowValueHasThousandsSeparators()
            => FactWithKey(_facts, "core.contextWindow").Value.ShouldBe("200,000");

        [Fact] // MF-3 AC1
        public void TheContextWindowSortValueIsTheTokenCount()
            => FactWithKey(_facts, "core.contextWindow").SortValue.ShouldBe(200000d);

        [Fact] // MF-3 AC2
        public void TheContextWindowLabelIsALocalizationKey()
            => FactWithKey(_facts, "core.contextWindow").Label.ShouldBe("#uaiModelFacts_contextWindow");
    }

    public class GivenUsdPricing
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> _facts =
            GetFacts(Descriptor(AIModelMetadata.ForPricing(new AIModelPricing(3.00m, 15.00m, "USD"))));

        [Fact] // MF-3 AC3
        public void TheModelHasACorePriceFact()
            => FactKeys(_facts).ShouldContain("core.price");

        [Fact] // MF-3 AC3
        public void ThePriceValueUsesTheDollarSymbol()
            => FactWithKey(_facts, "core.price").Value.ShouldBe("$3.00 / $15.00");

        [Fact] // MF-3 AC4
        public void ThePriceSortValueIsTheInputPrice()
            => FactWithKey(_facts, "core.price").SortValue.ShouldBe(3.0d);

        [Fact] // MF-3 AC6
        public void ThePriceDetailIsTheOutOfDateLocalizationKey()
            => FactWithKey(_facts, "core.price").Detail.ShouldBe("#uaiModelFacts_priceDetail");
    }

    public class GivenEurPricing
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> _facts =
            GetFacts(Descriptor(AIModelMetadata.ForPricing(new AIModelPricing(2.50m, 10.00m, "EUR"))));

        [Fact] // MF-3 AC5
        public void ThePriceValueUsesTheIsoCode()
            => FactWithKey(_facts, "core.price").Value.ShouldBe("EUR 2.50 / EUR 10.00");
    }

    public class GivenTheBuiltInProvider
    {
        [Fact] // MF-3 AC7
        public void CacheDurationIsZero()
            => new AIMetadataModelFactProvider().CacheDuration.ShouldBe(TimeSpan.Zero);
    }

    // ---------------------------------------------------------------- Sad path

    public class GivenOnlySettingsSupportMetadata
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> _facts =
            GetFacts(Descriptor(new AIModelSettingsSupport
            {
                UnsupportedProfileSettings = ["temperature"],
            }.ToMetadata()));

        [Fact] // MF-3 AC8
        public void TheResultHasNoEntryForTheModel()
            => _facts.ShouldNotContainKey(ModelId);
    }

    public class GivenAMalformedContextWindowAndPartialPricing
    {
        private readonly AIModelDescriptor _descriptor = Descriptor(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.ModelContextWindow] = "lots",
            [AIModelMetadataKeys.PricingInputPerMillionTokens] = "3.00",
        });

        [Fact] // MF-3 AC9
        public async Task GettingFactsDoesNotThrow()
            => await Should.NotThrowAsync(() => new AIMetadataModelFactProvider()
                .GetModelFactsAsync(Context, [_descriptor], CancellationToken.None));

        [Fact] // MF-3 AC9
        public void TheResultHasNoEntryForTheModel()
            => GetFacts(_descriptor).ShouldNotContainKey(ModelId);
    }
}
#endif
