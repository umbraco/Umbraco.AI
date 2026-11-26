// MEM-01 — Add-on settings on the AI Settings screen (AC1-AC4, AC7, AC8)

namespace Umbraco.AI.Tests.Unit.Api.Management.Settings;

// Real entry point: GetSettingsController / UpdateSettingsController + SettingsMapDefinition (as in
// DefaultDecisionProfileSettingsTests), over the real AISettingsService with an
// AISettingDefinitionCollection holding Test.Flag (bool, default true) and InMemoryAISettingsRepository.
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
