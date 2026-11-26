// MEM-20 — Builders choose per agent (AC1-AC3); MEM-19 — agents use it (AC7, AC8)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AC1-AC3: AIStandardAgentConfig and AgentMapDefinition (as in AgentMemoryModeTests). AC7, AC8: the
// agent run's runtime context builder (T36), checking ContextKeys.IncludePersonalContext it sets, and
// the site memory setting read through a fake IAISettingsService. AC4 (the toggle) is checked by
// T37's live acceptance: the Agent client has no component tests.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class AgentPersonalContextTests
{
    #region Happy path

    public class GivenANewAgent
    {
        public GivenANewAgent()
        {
            // new AIStandardAgentConfig() saved through the create mapping without the field.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void IncludePersonalContext_IsTrue()
        {
            // When: its config is read
            // Then: config.IncludePersonalContext.ShouldBeTrue();
        }
    }

    public class GivenStoredConfigWithoutTheField
    {
        public GivenStoredConfigWithoutTheField()
        {
            // Deserialize agent config JSON that has no includePersonalContext.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void IncludePersonalContext_ReadsTrue()
        {
            // When: it's read
            // Then: config.IncludePersonalContext.ShouldBeTrue();
        }
    }

    public class GivenAnAgentWithItTurnedOff
    {
        public GivenAnAgentWithItTurnedOff()
        {
            // Map an update request with StandardAgentConfigModel { IncludePersonalContext = false }.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Config_HasFalse()
        {
            // When: the agent is saved and read
            // Then: ((StandardAgentConfigModel)response.Config).IncludePersonalContext.ShouldBeFalse();
        }
    }

    public class GivenTheSiteMemorySwitchOff
    {
        public GivenTheSiteMemorySwitchOff()
        {
            // Agent.Memory.Enabled = false; agent IncludePersonalContext = true.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void Run_SetsTheFlag()
        {
            // When: the run's runtime context is built
            // Then: runtimeContext.GetValue<bool>(ContextKeys.IncludePersonalContext).ShouldBeTrue();
        }
    }

    #endregion

    #region Sad path

    public class GivenAnAgentWithItOff
    {
        public GivenAnAgentWithItOff()
        {
            // Agent IncludePersonalContext = false.
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void Run_DoesNotSetTheFlag()
        {
            // When: the run's runtime context is built
            // Then: runtimeContext.GetValue<bool?>(ContextKeys.IncludePersonalContext).ShouldNotBe(true);
        }
    }

    #endregion
}
