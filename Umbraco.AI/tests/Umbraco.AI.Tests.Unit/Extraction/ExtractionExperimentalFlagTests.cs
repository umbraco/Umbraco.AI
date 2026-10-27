// EX-3 — Extraction stays hidden until turned on (AC1-AC7). AC8 (the enabled-capabilities
// list) is in Api/Management/Capability/EnabledCapabilitiesExtractionTests; the controller-base gate
// attribute for AC9 is pinned in Api/Management/Extraction/ExtractControllerTests.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
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
/// flag gates it, per the add-ai-capability skill.
/// </summary>
public class ExtractionExperimentalFlagTests
{
    private const string ProviderId = "cloudflare";

    private static AIExperimentalFeatures Features(bool extractionEnabled)
    {
        var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(new AIExperimentalOptions { Extraction = extractionEnabled });
        return new AIExperimentalFeatures(monitor.Object);
    }

    /// <summary>A real <see cref="AIConnectionService"/> holding one connection to a provider that offers Extraction.</summary>
    private static (AIConnectionService Service, AIConnection Connection) ConnectionService(bool extractionEnabled)
    {
        var connection = new AIConnectionBuilder().WithProviderId(ProviderId).Build();
        var repository = new Mock<IAIConnectionRepository>();
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([connection]);

        var provider = new FakeAIProvider(ProviderId).WithCapability(new FakeExtractionCapability());
        var service = new AIConnectionService(
            repository.Object,
            new AIProviderCollection(() => [provider]),
            new Mock<IAIEditableModelResolver>().Object,
            new Mock<IAIEntityVersionService>().Object,
            new Mock<IEventAggregator>().Object,
            Features(extractionEnabled));

        return (service, connection);
    }

    /// <summary>A real <see cref="AIProfileService"/> whose repository saves whatever it is given.</summary>
    private static AIProfileService ProfileService(bool extractionEnabled)
    {
        var repository = new Mock<IAIProfileRepository>();
        repository
            .Setup(x => x.SaveAsync(It.IsAny<AIProfile>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile profile, Guid? _, CancellationToken _) => profile);

        var settingsService = new Mock<IAISettingsService>();
        settingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AISettings());

        var options = new Mock<IOptions<AIOptions>>();
        options.Setup(x => x.Value).Returns(new AIOptions());

        return new AIProfileService(
            repository.Object,
            settingsService.Object,
            options.Object,
            new Mock<IAIEntityVersionService>().Object,
            new Mock<IEventAggregator>().Object,
            Features(extractionEnabled));
    }

    private static AIProfile NewExtractionProfile()
        => new AIProfileBuilder().WithAlias("cloudflare-clairic").WithCapability(AICapability.Extraction).Build();

    private static ResourceExecutingContext GateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    #region Happy path

    public class GivenTheAICapabilityEnum
    {
        [Fact]
        public void GivesExtractionTheValue9() => ((int)AICapability.Extraction).ShouldBe(9);

        [Theory]
        [InlineData(AICapability.Chat, 0)]
        [InlineData(AICapability.Embedding, 1)]
        [InlineData(AICapability.Media, 2)]
        [InlineData(AICapability.Moderation, 3)]
        [InlineData(AICapability.SpeechToText, 4)]
        [InlineData(AICapability.ImageGeneration, 5)]
        [InlineData(AICapability.Decision, 8)]
        public void LeavesEveryExistingValueUnchanged(AICapability capability, int value) => ((int)capability).ShouldBe(value);
    }

    public class GivenNoConfiguration
    {
        [Fact]
        public void ReportsExtractionAsDisabled()
        {
            var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
            monitor.Setup(x => x.CurrentValue).Returns(new AIExperimentalOptions());

            new AIExperimentalFeatures(monitor.Object).IsCapabilityEnabled(AICapability.Extraction).ShouldBeFalse();
        }
    }

    public class GivenTheFlagOn
    {
        [Fact]
        public void ReportsExtractionAsEnabled() => Features(true).IsCapabilityEnabled(AICapability.Extraction).ShouldBeTrue();
    }

    public class GivenTheFlagOnAndAConnectionThatOffersExtraction
    {
        private readonly (AIConnectionService Service, AIConnection Connection) _arranged = ConnectionService(extractionEnabled: true);

        [Fact]
        public async Task ListsExtractionAmongTheCapabilities()
        {
            var capabilities = await _arranged.Service.GetAvailableCapabilitiesAsync();

            capabilities.ShouldContain(AICapability.Extraction);
        }

        [Fact]
        public async Task ReturnsTheConnectionForExtraction()
        {
            var connections = await _arranged.Service.GetConnectionsByCapabilityAsync(AICapability.Extraction);

            connections.ShouldContain(c => c.Id == _arranged.Connection.Id);
        }
    }

    public class GivenTheFlagOnAndANewExtractionProfile
    {
        private readonly AIProfileService _service = ProfileService(extractionEnabled: true);

        [Fact]
        public async Task SavesIt()
        {
            var saved = await _service.SaveProfileAsync(NewExtractionProfile());

            saved.Capability.ShouldBe(AICapability.Extraction);
        }
    }

    public class GivenTheFlagOnAndACallToTheExtractEndpoint
    {
        [Fact]
        public async Task LetsTheRequestThrough()
        {
            var context = GateContext();
            var filter = new AICapabilityGateFilter(AICapability.Extraction, Features(true));
            var nextInvoked = false;

            ResourceExecutionDelegate next = () =>
            {
                nextInvoked = true;
                return Task.FromResult(new ResourceExecutedContext(context, context.Filters));
            };

            await filter.OnResourceExecutionAsync(context, next);

            nextInvoked.ShouldBeTrue();
        }
    }

    #endregion

    #region Sad path (flag off)

    public class GivenTheFlagOffAndAConnectionThatOffersExtraction
    {
        private readonly (AIConnectionService Service, AIConnection Connection) _arranged = ConnectionService(extractionEnabled: false);

        [Fact]
        public async Task LeavesExtractionOutOfTheCapabilities()
        {
            var capabilities = await _arranged.Service.GetAvailableCapabilitiesAsync();

            capabilities.ShouldNotContain(AICapability.Extraction);
        }

        [Fact]
        public async Task ReturnsNoConnectionsForExtraction()
        {
            var connections = await _arranged.Service.GetConnectionsByCapabilityAsync(AICapability.Extraction);

            connections.ShouldBeEmpty();
        }
    }

    public class GivenTheFlagOffAndANewExtractionProfile
    {
        private readonly AIProfileService _service = ProfileService(extractionEnabled: false);

        [Fact]
        public async Task RejectsItWithInvalidOperationException()
        {
            var act = () => _service.SaveProfileAsync(NewExtractionProfile());

            await Should.ThrowAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task TellsTheCallerWhichFlagToTurnOn()
        {
            var exception = await Record.ExceptionAsync(() => _service.SaveProfileAsync(NewExtractionProfile()));

            exception!.Message.ShouldContain("Umbraco:AI:Experimental:Extraction");
        }
    }

    public class GivenTheFlagOffAndACallToTheExtractEndpoint
    {
        private readonly ResourceExecutingContext _context = GateContext();
        private bool _nextInvoked;

        public GivenTheFlagOffAndACallToTheExtractEndpoint()
        {
            var filter = new AICapabilityGateFilter(AICapability.Extraction, Features(false));

            ResourceExecutionDelegate next = () =>
            {
                _nextInvoked = true;
                return Task.FromResult(new ResourceExecutedContext(_context, _context.Filters));
            };

            filter.OnResourceExecutionAsync(_context, next).GetAwaiter().GetResult();
        }

        [Fact]
        public void Returns404() => _context.Result.ShouldBeOfType<NotFoundResult>();

        [Fact]
        public void NeverRunsTheEndpoint() => _nextInvoked.ShouldBeFalse();
    }

    #endregion
}
