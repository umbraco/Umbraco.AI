// DR-7 — Manage Decision profiles in the backoffice (AC4)

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Management.Common.Mapping;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Profile.Mapping;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Profile;

// Real ProfileMapDefinition through a real UmbracoMapper, then real System.Text.Json — the
// same path a GET profile response takes.
public class DecisionProfileSettingsMappingTests
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new ProfileMapDefinition(), new CommonMapDefinition() }),
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

        [Fact]
        public void MapsToADecisionSettingsModel() => _response.Settings.ShouldBeOfType<DecisionProfileSettingsModel>();

        [Fact]
        public void SerializesWithTheDecisionDiscriminator()
            => JsonSerializer.Serialize(_response.Settings, Options).ShouldContain("\"$type\":\"decision\"");
    }

    public class GivenACreateRequestForADecisionProfileWithNoSettings
    {
        [Fact]
        public void MapsToNonNullDecisionSettings()
        {
            var profile = CreateMapper().Map<AIProfile>(new CreateProfileRequestModel
            {
                Alias = "spam-check",
                Name = "Spam check",
                Capability = "Decision",
                Model = new ModelRefModel { ProviderId = "typesafe", ModelId = "jev-latest" },
                ConnectionId = Guid.NewGuid(),
            })!;

            profile.Settings.ShouldBeOfType<AIDecisionProfileSettings>();
        }
    }

    public class GivenADollarTypeDecisionSettingsPayload
    {
        [Fact]
        public void DeserializesToADecisionProfileSettingsModel()
        {
            const string json = """{"$type":"decision"}""";

            var settings = JsonSerializer.Deserialize<ProfileSettingsModel>(json, Options);

            settings.ShouldBeOfType<DecisionProfileSettingsModel>();
        }
    }

    #endregion
}
