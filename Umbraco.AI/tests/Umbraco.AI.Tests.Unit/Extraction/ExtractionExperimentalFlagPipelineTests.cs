#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-3 (AC4, AC10): flag on and off through the real extraction service.

using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Every surface is built on a real <see cref="AIExperimentalFeatures"/> (not a mocked
/// <see cref="IAIExperimentalFeatures"/>), so the specs prove the real <c>Umbraco:AI:Experimental:Extraction</c>
/// flag gates it, per the add-ai-capability skill. The service specs go through the real
/// <see cref="AIExtractionService"/> + <see cref="AIExtractionClientFactory"/> (see <see cref="ExtractionPipelineHarness"/>).
/// </summary>
public class ExtractionExperimentalFlagPipelineTests
{
    private static AITextExtractionField OrderNumber() => new() { Instructions = "The order number" };

    public class GivenTheFlagOnAndAnExtractionCall
    {
        private readonly FakeExtractionClient _client = new();
        private readonly ExtractionPipelineHarness _harness;

        public GivenTheFlagOnAndAnExtractionCall() => _harness = new ExtractionPipelineHarness(_client, extractionEnabled: true);

        [Fact]
        public async Task ReachesTheProvider()
        {
            await _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), "Order A-88412");

            _client.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenTheFlagOffAndAnExtractionCall
    {
        private readonly FakeExtractionClient _client = new();
        private readonly ExtractionPipelineHarness _harness;

        public GivenTheFlagOffAndAnExtractionCall() => _harness = new ExtractionPipelineHarness(_client, extractionEnabled: false);

        [Fact]
        public async Task ThrowsInvalidOperationException()
        {
            var act = () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), "Order A-88412");

            await Should.ThrowAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task NeverReachesTheProvider()
        {
            await Record.ExceptionAsync(
                () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), "Order A-88412"));

            _client.ReceivedRequests.ShouldBeEmpty();
        }
    }

}
