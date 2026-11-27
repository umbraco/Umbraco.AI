// MEM-01 — Add-on settings on the AI Settings screen (AC1-AC4, AC7-AC12)

namespace Umbraco.AI.Tests.Unit.Api.Management.Settings;

// Real entry point: GetSettingsController / UpdateSettingsController + SettingsMapDefinition (as in
// DefaultDecisionProfileSettingsTests), over the real AISettingsService with an
// AISettingDefinitionCollection holding Test.Flag (bool, default true) and InMemoryAISettingsRepository.
//
// AC11 is a cache-refresher check: no CMS integration harness exists in the unit project, so it is
// kept here as a pending spec and the builder may move it to the integration project (cache
// refreshers need UmbracoIntegrationTest with caching switched on). AC12 compiles against an
// IAISettingsService implementation that predates GetSettingValueAsync<T>.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class AddOnSettingsTests
{
    #region Happy path

    public class GivenARegisteredSettingWithNothingSaved
    {
        public GivenARegisteredSettingWithNothingSaved()
        {
            // Register AISettingDefinition("Test.Flag", typeof(bool), default: true); nothing saved.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void GetSettings_ReturnsTheDefault()
        {
            // When: GET settings
            // Then: response.Additional["Test.Flag"].ShouldBe(true);
        }
    }

    public class GivenARegisteredSettingSavedAsFalse
    {
        public GivenARegisteredSettingSavedAsFalse()
        {
            // Register Test.Flag; PUT settings with Additional = { "Test.Flag": false }.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void GetSettings_ReturnsTheSavedValue()
        {
            // When: GET settings again
            // Then: response.Additional["Test.Flag"].ShouldBe(false);
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void GetSettingValueAsync_ReturnsTheSavedValue()
        {
            // When: IAISettingsService.GetSettingValueAsync<bool>("Test.Flag")
            // Then: value.ShouldBeFalse();
        }
    }

    public class GivenAddOnSettingsAreRegistered
    {
        public GivenAddOnSettingsAreRegistered()
        {
            // Register Test.Flag; PUT settings with a new DefaultChatProfileId (a Chat profile).
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void GetSettings_ReturnsTheNewDefaultChatProfile()
        {
            // When: GET settings again
            // Then: response.DefaultChatProfileId.ShouldBe(newProfileId);
        }
    }

    public class GivenAStoredRowNoPackageRegisters
    {
        public GivenAStoredRowNoPackageRegisters()
        {
            // Seed the repository with a stored row "Old.Addon.Flag"; no definition registers it.
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void GetSettings_LeavesTheRowOut()
        {
            // When: GET settings
            // Then: response.Additional.ShouldNotContainKey("Old.Addon.Flag");
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void UpdateSettings_LeavesTheRowUnchanged()
        {
            // When: PUT settings (any valid body), then read the stored row from the repository
            // Then: storedRows["Old.Addon.Flag"].ShouldBe(originalValue);
        }
    }

    public class GivenTwoServersSharingOneDatabase
    {
        public GivenTwoServersSharingOneDatabase()
        {
            // Two AISettingsService instances over one repository, each with settings cached; Test.Flag read on both.
        }

        // AC11
        [Fact(Skip = "Pending")]
        public void SecondServer_ReadsTheNewValueAfterASaveOnTheFirst()
        {
            // When: PUT Test.Flag = false on the first server, and the cache refresher runs for the second
            // Then: (await secondServer.GetSettingValueAsync<bool>("Test.Flag")).ShouldBeFalse();
        }
    }

    public class GivenAnOlderIAISettingsServiceImplementation
    {
        public GivenAnOlderIAISettingsServiceImplementation()
        {
            // A test class implementing IAISettingsService without GetSettingValueAsync<T> (it compiles).
        }

        // AC12
        [Fact(Skip = "Pending")]
        public async Task GetSettingValueAsync_ThrowsNotSupported()
        {
            // When: older.GetSettingValueAsync<bool>("Test.Flag")
            // Then: await Should.ThrowAsync<NotSupportedException>(() => older.GetSettingValueAsync<bool>("Test.Flag"));
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Sad path

    public class GivenAnUnregisteredKey
    {
        public GivenAnUnregisteredKey()
        {
            // No definition for Test.Unknown.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void UpdateSettings_ReturnsBadRequest()
        {
            // When: PUT settings with Additional = { "Test.Unknown": 1 }
            // Then: result.ShouldBeAssignableTo<BadRequestObjectResult>();
        }
    }

    public class GivenARegisteredBoolSetting
    {
        public GivenARegisteredBoolSetting()
        {
            // Register Test.Flag as bool.
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void UpdateSettings_WithAString_ReturnsBadRequest()
        {
            // When: PUT settings with Additional = { "Test.Flag": "yes" }
            // Then: result.ShouldBeAssignableTo<BadRequestObjectResult>();
        }
    }

    #endregion
}
