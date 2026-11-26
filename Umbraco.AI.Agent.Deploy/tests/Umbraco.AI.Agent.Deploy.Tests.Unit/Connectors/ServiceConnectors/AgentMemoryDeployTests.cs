// MEM-22 — Memory and personal context settings move with Deploy (AC5, AC6, AC11, AC12)
using Xunit;

namespace Umbraco.AI.Agent.Deploy.Tests.Unit.Connectors.ServiceConnectors;

// Real entry point: UmbracoAIAgentServiceConnector's export and import, set up the way
// UmbracoAIAgentServiceConnectorTests does, with a memory store fake holding the agent's memories.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class AgentMemoryDeployTests
{
    #region Happy path

    public class GivenAnAgentWithMemoryModeWork
    {
        // AC5
        [Fact(Skip = "Pending")]
        public void RoundTrip_KeepsMemoryModeWork()
        {
            // When: the connector exports the agent and imports the artifact
            // Then: ((AIStandardAgentConfig)imported.Config).MemoryMode.ShouldBe(AIMemoryMode.Work);
        }
    }

    public class GivenAnAgentWithIncludePersonalContextOff
    {
        // AC6
        [Fact(Skip = "Pending")]
        public void RoundTrip_KeepsIncludePersonalContextFalse()
        {
            // When: the connector exports the agent and imports the artifact
            // Then: ((AIStandardAgentConfig)imported.Config).IncludePersonalContext.ShouldBeFalse();
        }
    }

    #endregion

    #region Sad path

    public class GivenAnOlderAgentArtifact
    {
        // AC11
        [Fact(Skip = "Pending")]
        public void Import_ReadsIncludePersonalContextAsTrue()
        {
            // Given: artifact.Config JSON has no includePersonalContext
            // When: the connector imports the artifact
            // Then: ((AIStandardAgentConfig)imported.Config).IncludePersonalContext.ShouldBeTrue();
        }
    }

    public class GivenAnAgentWithMemories
    {
        // AC12
        [Fact(Skip = "Pending")]
        public void Export_HoldsNoMemoryText()
        {
            // Given: a "you + this agent" memory "Prefers British spelling"
            // When: the connector exports the agent
            // Then: JsonSerializer.Serialize(artifact).ShouldNotContain("Prefers British spelling");
        }
    }

    #endregion
}
