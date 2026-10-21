// EX-5 — Manage Extraction profiles in the backoffice (AC3, backend half)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental Extraction profile settings

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

// Copies DecisionProfileSettingsMappingTests: real ProfileMapDefinition through a real UmbracoMapper, then
// real System.Text.Json, the same path a create and a GET profile response take.
public class ExtractionProfileSettingsMappingTests
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new ProfileMapDefinition(), new CommonMapDefinition() }),
        Mock.Of<ICoreScopeProvider>(),
        NullLogger<UmbracoMapper>.Instance);

    #region Happy path

    public class GivenAnExtractionProfile
    {
        private readonly ProfileResponseModel _response;

        public GivenAnExtractionProfile()
        {
            var profile = new AIProfile
            {
                Alias = "cloudflare-clairic",
                Name = "Cloudflare Clairic",
                ConnectionId = Guid.NewGuid(),
                Capability = AICapability.Extraction,
                Settings = new AIExtractionProfileSettings(),
            };
            _response = CreateMapper().Map<ProfileResponseModel>(profile)!;
        }

        [Fact(Skip = "Pending")]
        public void MapsToAnExtractionSettingsModel() => _response.Settings.ShouldBeOfType<ExtractionProfileSettingsModel>();

        [Fact(Skip = "Pending")]
        public void SerializesWithTheExtractionDiscriminator()
            => JsonSerializer.Serialize(_response.Settings, Options).ShouldContain("\"$type\":\"extraction\"");

        [Fact(Skip = "Pending")]
        public void MapsTheCapabilityName() => _response.Capability.ShouldBe("Extraction");
    }

    public class GivenACreateRequestForAnExtractionProfileWithNoSettings
    {
        [Fact(Skip = "Pending")]
        public void MapsToNonNullExtractionSettings()
        {
            var profile = CreateMapper().Map<AIProfile>(new CreateProfileRequestModel
            {
                Alias = "cloudflare-clairic",
                Name = "Cloudflare Clairic",
                Capability = "Extraction",
                Model = new ModelRefModel { ProviderId = "cloudflare", ModelId = "@cf/cloudflare/clairic" },
                ConnectionId = Guid.NewGuid(),
            })!;

            profile.Settings.ShouldBeOfType<AIExtractionProfileSettings>();
        }
    }

    public class GivenAnExtractionProfileSavedAndReopened
    {
        private readonly Guid _connectionId = Guid.NewGuid();
        private readonly ProfileResponseModel _reopened;

        public GivenAnExtractionProfileSavedAndReopened()
        {
            var mapper = CreateMapper();
            var saved = mapper.Map<AIProfile>(new CreateProfileRequestModel
            {
                Alias = "cloudflare-clairic",
                Name = "Cloudflare Clairic",
                Capability = "Extraction",
                Model = new ModelRefModel { ProviderId = "cloudflare", ModelId = "@cf/cloudflare/clairic" },
                ConnectionId = _connectionId,
            })!;
            _reopened = mapper.Map<ProfileResponseModel>(saved)!;
        }

        [Fact(Skip = "Pending")]
        public void KeepsItsConnection() => _reopened.ConnectionId.ShouldBe(_connectionId);

        [Fact(Skip = "Pending")]
        public void KeepsItsProvider() => _reopened.Model!.ProviderId.ShouldBe("cloudflare");

        [Fact(Skip = "Pending")]
        public void KeepsItsModel() => _reopened.Model!.ModelId.ShouldBe("@cf/cloudflare/clairic");

        [Fact(Skip = "Pending")]
        public void KeepsItsEmptyExtractionSettings() => _reopened.Settings.ShouldBeOfType<ExtractionProfileSettingsModel>();
    }

    public class GivenADollarTypeExtractionSettingsPayload
    {
        [Fact(Skip = "Pending")]
        public void DeserializesToAnExtractionProfileSettingsModel()
        {
            const string json = """{"$type":"extraction"}""";

            var settings = JsonSerializer.Deserialize<ProfileSettingsModel>(json, Options);

            settings.ShouldBeOfType<ExtractionProfileSettingsModel>();
        }
    }

    #endregion
}
