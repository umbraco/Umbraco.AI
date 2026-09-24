// DR-7 — Manage Decision profiles in the backoffice (AC4)
#pragma warning disable UMBRACOAI_DECISION

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Management.Profile.Mapping;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Profile;

// Real ProfileMapDefinition through a real UmbracoMapper, then real System.Text.Json — the
// same path a GET profile response takes.
public class DecisionProfileSettingsMappingTests
{
    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new ProfileMapDefinition() }),
        Mock.Of<ICoreScopeProvider>(),
        NullLogger<UmbracoMapper>.Instance);

    #region Happy path

    public class GivenADecisionProfile
    {
        private readonly ProfileResponseModel _response;

        public GivenADecisionProfile()
        {
            var profile = new AIProfile
            {
                Alias = "spam-check",
                Name = "Spam check",
                ConnectionId = Guid.NewGuid(),
                Capability = AICapability.Decision,
                Settings = new AIDecisionProfileSettings(),
            };
            _response = CreateMapper().Map<ProfileResponseModel>(profile)!;
        }

        [Fact(Skip = "Pending T10")]
        public void MapsToADecisionSettingsModel() => _response.Settings.ShouldBeOfType<DecisionProfileSettingsModel>();

        [Fact(Skip = "Pending T10")]
        public void SerializesWithTheDecisionDiscriminator()
            => JsonSerializer.Serialize(_response.Settings).ShouldContain("\"$type\":\"decision\"");
    }

    public class GivenACreateRequestForADecisionProfileWithNoSettings
    {
        [Fact(Skip = "Pending T10")]
        public void MapsToNonNullDecisionSettings()
        {
            var profile = CreateMapper().Map<AIProfile>(new CreateProfileRequestModel
            {
                Alias = "spam-check",
                Name = "Spam check",
                Capability = "Decision",
                ConnectionId = Guid.NewGuid(),
            })!;

            profile.Settings.ShouldBeOfType<AIDecisionProfileSettings>();
        }
    }

    #endregion
}
