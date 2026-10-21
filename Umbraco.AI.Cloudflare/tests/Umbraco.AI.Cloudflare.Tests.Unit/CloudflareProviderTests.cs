// EX-2 — Connect to Cloudflare (AC1-AC6, AC28; AC1's provider-listing half is the generic Web provider listing)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

public class CloudflareProviderTests
{
    private static PropertyInfo Property(string name) => typeof(CloudflareProviderSettings).GetProperty(name)!;

    #region Happy path

    public class GivenTheProvider
    {
        private readonly CloudflareProvider _provider = CloudflareTestHost.CreateProvider();

        [Fact]
        public void HasTheCloudflareProviderId()
        {
            _provider.Id.ShouldBe("cloudflare");
        }

        [Fact]
        public void HasTheCloudflareDisplayName()
        {
            _provider.Name.ShouldBe("Cloudflare");
        }

        [Fact]
        public void ExposesOnlyTheExtractionCapability()
        {
            _provider.GetCapabilities().Select(c => c.Kind).ShouldBe([AICapability.Extraction]);
        }
    }

    public class GivenTheConnectionSettings
    {
        [Fact]
        public void AreAccountIdApiTokenAndEndpoint()
        {
            typeof(CloudflareProviderSettings)
                .GetProperties()
                .Where(p => p.GetCustomAttribute<AIFieldAttribute>() is not null)
                .Select(p => p.Name)
                .ShouldBe(["AccountId", "ApiToken", "Endpoint"], ignoreOrder: true);
        }

        [Fact]
        public void MarksTheAccountIdAsRequired()
        {
            Property("AccountId").GetCustomAttribute<RequiredAttribute>().ShouldNotBeNull();
        }

        [Fact]
        public void MarksTheApiTokenAsRequired()
        {
            Property("ApiToken").GetCustomAttribute<RequiredAttribute>().ShouldNotBeNull();
        }

        [Fact]
        public void MarksTheApiTokenAsSensitiveSoItIsMaskedAndEncrypted()
        {
            // AC4: masking on read-back and encryption at rest are the generic sensitive-field handling
            // (AIEditableModelSerializer / masked editor), driven only by this flag.
            Property("ApiToken").GetCustomAttribute<AIFieldAttribute>()!.IsSensitive.ShouldBeTrue();
        }

        [Fact]
        public void DoesNotMarkTheAccountIdAsSensitive()
        {
            Property("AccountId").GetCustomAttribute<AIFieldAttribute>()!.IsSensitive.ShouldBeFalse();
        }
    }

    public class GivenSettingsWithNoEndpoint
    {
        [Fact]
        public void DefaultsTheEndpointToTheCloudflareApi()
        {
            new CloudflareProviderSettings().Endpoint.ShouldBe("https://api.cloudflare.com/client/v4");
        }
    }

    public class GivenAValidConnection
    {
        [Fact]
        public async Task ListsClairicAsItsOnlyModel()
        {
            var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));
            var capability = CloudflareTestHost.CreateCapability(handler);

            var models = await capability.GetModelsAsync(CloudflareTestHost.Settings());

            models.Select(m => m.Model.ModelId).ShouldBe(["@cf/cloudflare/clairic"]);
        }
    }

    #endregion

    #region Sad path

    public class GivenSettingsWithNoAccountId
    {
        private readonly ScriptedHttpMessageHandler _handler = new(ScriptedHttpMessageHandler.Json(CloudflareTestHost.ModelSearchAnswer));
        private readonly IAIExtractionCapability _capability;

        public GivenSettingsWithNoAccountId()
            => _capability = CloudflareTestHost.CreateCapability(_handler);

        [Fact]
        public void IsRejectedAsARequiredFieldOnSave()
        {
            // Saving a connection validates settings with DataAnnotations (AIEditableModelResolver).
            var results = new List<ValidationResult>();
            var settings = new CloudflareProviderSettings { ApiToken = CloudflareTestHost.ApiToken };

            Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);

            results.ShouldContain(r => r.MemberNames.Contains("AccountId"));
        }

        [Fact]
        public async Task CreatingAClientFailsBeforeAnyNetworkCall()
        {
            await Should.ThrowAsync<InvalidOperationException>(
                () => _capability.CreateClientAsync(new CloudflareProviderSettings { ApiToken = CloudflareTestHost.ApiToken }, null, default));
        }

        [Fact]
        public async Task NeverReachesTheHandler()
        {
            await Record.ExceptionAsync(
                () => _capability.CreateClientAsync(new CloudflareProviderSettings { ApiToken = CloudflareTestHost.ApiToken }, null, default));

            _handler.Requests.ShouldBeEmpty();
        }
    }

    public class GivenSettingsWithNoApiToken
    {
        [Fact]
        public async Task CreatingAClientFailsBeforeAnyNetworkCall()
        {
            var capability = CloudflareTestHost.CreateCapability();

            await Should.ThrowAsync<InvalidOperationException>(
                () => capability.CreateClientAsync(CloudflareTestHost.Settings(apiToken: null), null, default));
        }
    }

    #endregion
}
