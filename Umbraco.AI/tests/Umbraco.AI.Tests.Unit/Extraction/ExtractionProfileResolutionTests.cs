#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC17-AC20, AC37): which profile a call runs against.

using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Every spec goes through the real <see cref="AIExtractionService"/> + <see cref="AIExtractionClientFactory"/>
/// pipeline (see <see cref="ExtractionPipelineHarness"/>); only the provider client is faked. How the default
/// profile itself is chosen (stored id, then config alias) is in <see cref="DefaultExtractionProfileTests"/>.
/// </summary>
public class ExtractionProfileResolutionTests
{
    private static AITextExtractionField OrderNumber() => new() { Instructions = "The order number" };

    private const string State = "Order A-88412 was delivered on 3 October.";

    #region Happy path

    public class GivenAProfileId
    {
        private readonly ExtractionPipelineHarness _harness = new(new FakeExtractionClient());

        [Fact]
        public async Task RunsAgainstThatProfile()
        {
            await _harness.Service.ExtractAsync(_harness.Profile.Id, OrderNumber(), State);

            _harness.ProfileServiceMock.Verify(
                x => x.GetProfileAsync(_harness.Profile.Id, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
    }

    public class GivenAProfileAlias
    {
        private readonly ExtractionPipelineHarness _harness = new(new FakeExtractionClient());

        [Fact]
        public async Task RunsAgainstThatProfile()
        {
            await _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), State);

            _harness.ProfileServiceMock.Verify(
                x => x.GetProfileByAliasAsync(ExtractionPipelineHarness.ProfileAlias, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    public class GivenABuilderNamingAProfileAlias
    {
        private readonly ExtractionPipelineHarness _harness = new(new FakeExtractionClient());

        [Fact]
        public async Task RunsAgainstThatProfile()
        {
            await _harness.Service.ExtractAsync(b => b.WithAlias("order-extract").WithProfile(ExtractionPipelineHarness.ProfileAlias), OrderNumber(), State);

            _harness.ProfileServiceMock.Verify(
                x => x.GetProfileByAliasAsync(ExtractionPipelineHarness.ProfileAlias, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    public class GivenNoProfile
    {
        private readonly ExtractionPipelineHarness _harness = new(new FakeExtractionClient());

        [Fact]
        public async Task RunsAgainstTheDefaultExtractionProfile()
        {
            await _harness.Service.ExtractAsync(OrderNumber(), State);

            _harness.ProfileServiceMock.Verify(
                x => x.GetDefaultProfileAsync(AICapability.Extraction, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion

    #region Sad path

    public class GivenAChatProfileAlias
    {
        private readonly FakeExtractionClient _client = new();
        private readonly ExtractionPipelineHarness _harness;

        public GivenAChatProfileAlias() => _harness = new ExtractionPipelineHarness(_client, profileCapability: AICapability.Chat);

        [Fact]
        public async Task ThrowsInvalidOperationException()
        {
            var act = () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), State);

            await Should.ThrowAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task NeverReachesTheProvider()
        {
            await Record.ExceptionAsync(
                () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), State));

            _client.ReceivedRequests.ShouldBeEmpty();
        }
    }

    #endregion
}
