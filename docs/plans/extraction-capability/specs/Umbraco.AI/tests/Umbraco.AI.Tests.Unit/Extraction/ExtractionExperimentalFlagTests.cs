#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-3 (AC4, AC10): flag on and off through the real extraction service. Moves in with T3.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Management.Common.Filters;
using Umbraco.Cms.Core.Events;

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

        [Fact(Skip = "Pending")]
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

        [Fact(Skip = "Pending")]
        public async Task ThrowsInvalidOperationException()
        {
            var act = () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), "Order A-88412");

            await Should.ThrowAsync<InvalidOperationException>(act);
        }

        [Fact(Skip = "Pending")]
        public async Task NeverReachesTheProvider()
        {
            await Record.ExceptionAsync(
                () => _harness.Service.ExtractAsync(ExtractionPipelineHarness.ProfileAlias, OrderNumber(), "Order A-88412"));

            _client.ReceivedRequests.ShouldBeEmpty();
        }
    }

}
