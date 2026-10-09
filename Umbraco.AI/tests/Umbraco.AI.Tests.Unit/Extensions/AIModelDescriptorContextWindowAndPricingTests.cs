#if MODEL_FACTS_PENDING // Pending: T1 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-2: Providers can declare a model's context window and price (AC1-AC6).
// Writer helper assumed as AIModelMetadata.ForContextWindow(int) / AIModelMetadata.ForPricing(AIModelPricing),
// each returning IReadOnlyDictionary<string, string>, mirroring AIModelSettingsSupport.ToMetadata().
using System.Globalization;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Tests.Unit.Extensions;

public class AIModelDescriptorContextWindowAndPricingTests
{
    private static AIModelDescriptor Descriptor(IReadOnlyDictionary<string, string> metadata)
        => new(new AIModelRef("openrouter", "anthropic/claude-sonnet-4"), "Claude Sonnet 4", metadata);

    // ---------------------------------------------------------------- Happy path

    public class GivenMetadataWrittenWithAContextWindowOf200000
    {
        private readonly AIModelDescriptor _descriptor =
            Descriptor(AIModelMetadata.ForContextWindow(200000));

        [Fact] // MF-2 AC1
        public void GetContextWindow_ReturnsTheWrittenValue()
            => _descriptor.GetContextWindow().ShouldBe(200000);
    }

    public class GivenMetadataWrittenWithUsdPricing
    {
        private readonly AIModelDescriptor _descriptor =
            Descriptor(AIModelMetadata.ForPricing(new AIModelPricing(3.00m, 15.00m, "USD")));

        [Fact] // MF-2 AC2
        public void GetPricing_ReturnsTheWrittenPricing()
            => _descriptor.GetPricing().ShouldBe(new AIModelPricing(3.00m, 15.00m, "USD"));
    }

    public class GivenTheCurrentCultureIsDanish : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly IReadOnlyDictionary<string, string> _metadata;

        public GivenTheCurrentCultureIsDanish()
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            _metadata = AIModelMetadata.ForPricing(new AIModelPricing(3.5m, 15m, "USD"));
        }

        public void Dispose() => CultureInfo.CurrentCulture = _originalCulture;

        [Fact] // MF-2 AC3
        public void PricingIsWrittenInInvariantCulture()
            => _metadata[AIModelMetadataKeys.PricingInputPerMillionTokens].ShouldBe("3.5");
    }

    // ---------------------------------------------------------------- Sad path

    public class GivenNoContextWindowKey
    {
        private readonly AIModelDescriptor _descriptor = Descriptor(new Dictionary<string, string>());

        [Fact] // MF-2 AC4
        public void GetContextWindow_ReturnsNull()
            => _descriptor.GetContextWindow().ShouldBeNull();
    }

    public class GivenAMalformedContextWindow
    {
        private readonly AIModelDescriptor _descriptor = Descriptor(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.ModelContextWindow] = "lots",
        });

        [Fact] // MF-2 AC5
        public void GetContextWindow_ReturnsNull()
            => _descriptor.GetContextWindow().ShouldBeNull();
    }

    public class GivenPartialPricing
    {
        private readonly AIModelDescriptor _descriptor = Descriptor(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.PricingInputPerMillionTokens] = "3.00",
            [AIModelMetadataKeys.PricingCurrency] = "USD",
        });

        [Fact] // MF-2 AC6
        public void GetPricing_ReturnsNull()
            => _descriptor.GetPricing().ShouldBeNull();
    }
}
#endif
