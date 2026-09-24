// DR-7 — Manage Decision profiles in the backoffice (AC3: settings serialize)

using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Tests.Unit.Profiles;

public class AIProfileSettingsSerializerDecisionTests
{
    public class GivenSerializedDecisionProfileSettings
    {
        private readonly string? _json = AIProfileSettingsSerializer.Serialize(new AIDecisionProfileSettings());

        [Fact]
        public void DeserializesToDecisionProfileSettings()
        {
            var settings = AIProfileSettingsSerializer.Deserialize(AICapability.Decision, _json);

            settings.ShouldBeOfType<AIDecisionProfileSettings>();
        }
    }

    public class GivenAnEmptyJsonObjectForADecisionProfile
    {
        [Fact]
        public void DeserializesToDecisionProfileSettings()
        {
            var settings = AIProfileSettingsSerializer.Deserialize(AICapability.Decision, "{}");

            settings.ShouldBeOfType<AIDecisionProfileSettings>();
        }
    }
}
