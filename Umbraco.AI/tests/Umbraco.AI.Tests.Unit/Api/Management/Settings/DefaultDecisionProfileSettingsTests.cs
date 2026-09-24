// DR-3 — Set a default Decision profile (AC1)
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Controllers;
using Umbraco.AI.Web.Api.Management.Settings.Mapping;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Settings;

// Real UpdateSettingsController + real SettingsMapDefinition; the settings service is the
// mocked collaborator and echoes back what it was asked to save (a round trip through the
// same mapping GET uses).
public class DefaultDecisionProfileSettingsTests
{
    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new SettingsMapDefinition() }),
        Mock.Of<ICoreScopeProvider>(),
        NullLogger<UmbracoMapper>.Instance);

    #region Happy path

    public class GivenADecisionProfileIsPutAsTheDefault
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly Mock<IAISettingsService> _settingsService = new();
        private readonly IActionResult _result;
        private AISettings? _saved;

        public GivenADecisionProfileIsPutAsTheDefault()
        {
            _settingsService
                .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
                .Callback<AISettings, CancellationToken>((s, _) => _saved = s)
                .ReturnsAsync((AISettings s, CancellationToken _) => s);

            _result = new UpdateSettingsController(_settingsService.Object, CreateMapper())
                .UpdateSettings(new UpdateSettingsRequestModel { DefaultDecisionProfileId = _profileId })
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void SavesTheDefaultDecisionProfileId() => _saved!.DefaultDecisionProfileId.ShouldBe(_profileId);

        [Fact]
        public void ReturnsItInTheResponse()
            => ((SettingsResponseModel)((OkObjectResult)_result).Value!).DefaultDecisionProfileId.ShouldBe(_profileId);
    }

    #endregion
}
