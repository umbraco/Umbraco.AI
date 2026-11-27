// MEM-22 — Memory and personal context settings move with Deploy (AC1-AC4, AC7-AC10, AC13)
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

// Real entry point: UmbracoAISettingsServiceConnector's GetArtifactAsync (export) and ProcessAsync
// pass 3 (import), with a settings service fake that knows the registered add-on definitions.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class SettingsMemoryDeployTests
{
    #region Happy path

    public class GivenThePersonalContextSwitchIsOff
    {
        // AC1
        [Fact(Skip = "Pending")]
        public void Export_CarriesPersonalContextEnabledFalse()
        {
            // Given: settings.PersonalContextEnabled = false
            // When: the connector exports AI Settings
            // Then: artifact.PersonalContextEnabled.ShouldBe(false);
        }
    }

    public class GivenAnArtifactWithPersonalContextEnabledFalse
    {
        // AC2
        [Fact(Skip = "Pending")]
        public void Import_SetsPersonalContextEnabledFalse()
        {
            // When: the connector imports the artifact
            // Then: savedSettings.PersonalContextEnabled.ShouldBeFalse();
        }
    }

    public class GivenAnAddOnSettingIsSaved
    {
        // AC3
        [Fact(Skip = "Pending")]
        public void Export_CarriesTheAddOnValue()
        {
            // Given: "Agent.Memory.Enabled" registered (bool) and saved as false
            // When: the connector exports AI Settings
            // Then: artifact.Additional["Agent.Memory.Enabled"] is false
        }
    }

    public class GivenAnArtifactWithAnAddOnValueAndTheAddOnInstalled
    {
        // AC4
        [Fact(Skip = "Pending")]
        public void Import_SavesTheAddOnValue()
        {
            // Given: artifact.Additional = { "Agent.Memory.PersonalMemory": "OptIn" }; target registers it
            // When: the connector imports the artifact
            // Then: (await settingsService.GetSettingValueAsync<string>("Agent.Memory.PersonalMemory")).ShouldBe("OptIn");
        }
    }

    public class GivenALoadBalancedTarget
    {
        // AC13
        [Fact(Skip = "Pending")]
        public void Import_RefreshesEveryServersSettings()
        {
            // Given: two settings service instances over one store, both with settings cached
            // When: the connector imports AI Settings through the first
            // Then: (await secondServer.GetSettingValueAsync<bool>("Agent.Memory.Enabled")).ShouldBeFalse();
        }
    }

    #endregion

    #region Sad path

    public class GivenAnOlderArtifactWithNoPersonalContextValue
    {
        // AC7
        [Fact(Skip = "Pending")]
        public void Import_KeepsTheTargetsSwitch()
        {
            // Given: artifact.PersonalContextEnabled = null; target switch off
            // When: the connector imports the artifact
            // Then: savedSettings.PersonalContextEnabled.ShouldBeFalse();
        }
    }

    public class GivenAnAddOnKeyTheTargetHasNotRegistered
    {
        // Arrange: artifact.Additional = { "Agent.Memory.Enabled": false } plus a default chat
        // profile; the target registers no add-on settings.

        // AC8
        [Fact(Skip = "Pending")]
        public void Import_StoresNoValueForTheKey()
        {
            // Then: savedSettings.Additional.ShouldNotContainKey("Agent.Memory.Enabled");
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void Import_StillSetsTheDefaultChatProfile()
        {
            // Then: savedSettings.DefaultChatProfileId.ShouldBe(chatProfileId);
        }
    }

    public class GivenAnAddOnValueOfTheWrongType
    {
        // AC10
        [Fact(Skip = "Pending")]
        public void Import_KeepsTheTargetsValue()
        {
            // Given: target registers "Agent.Memory.Enabled" (bool), saved true; artifact has "yes"
            // When: the connector imports the artifact
            // Then: (await settingsService.GetSettingValueAsync<bool>("Agent.Memory.Enabled")).ShouldBeTrue();
        }
    }

    #endregion
}
