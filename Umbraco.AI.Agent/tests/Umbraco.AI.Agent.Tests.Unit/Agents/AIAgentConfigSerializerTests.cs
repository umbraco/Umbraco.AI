using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Orchestrations;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentConfigSerializerTests
{
    #region Serialize

    [Fact]
    public void Serialize_NullConfig_ReturnsNull()
    {
        var result = AIAgentConfigSerializer.Serialize(null);
        result.ShouldBeNull();
    }

    [Fact]
    public void Serialize_StandardConfig_ReturnsJson()
    {
        var config = new AIStandardAgentConfig
        {
            Instructions = "Be helpful",
            AllowedToolIds = ["tool1", "tool2"],
            AllowedToolScopeIds = ["content-read"],
        };

        var result = AIAgentConfigSerializer.Serialize(config);

        result.ShouldNotBeNull();
        result.ShouldContain("\"instructions\":\"Be helpful\"");
        result.ShouldContain("\"allowedToolIds\":[\"tool1\",\"tool2\"]");
    }

    [Fact]
    public void Serialize_OrchestratedConfig_ReturnsJson()
    {
        var config = new AIOrchestratedAgentConfig
        {
            Graph = new AIOrchestrationGraph
            {
                Nodes = [new AIOrchestrationNode { Id = "node1", Type = AIOrchestrationNodeType.Agent, Label = "Agent 1" }],
            },
        };

        var result = AIAgentConfigSerializer.Serialize(config);

        result.ShouldNotBeNull();
        result.ShouldContain("\"nodes\"");
        result.ShouldContain("\"node1\"");
    }

    #endregion

    #region Deserialize

    [Fact]
    public void Deserialize_NullJson_StandardType_ReturnsDefaultStandardConfig()
    {
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Standard, null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<AIStandardAgentConfig>();
    }

    [Fact]
    public void Deserialize_NullJson_OrchestratedType_ReturnsDefaultOrchestratedConfig()
    {
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Orchestrated, null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<AIOrchestratedAgentConfig>();
    }

    [Fact]
    public void Deserialize_EmptyJson_ReturnsDefaultConfig()
    {
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Standard, "");

        result.ShouldNotBeNull();
        result.ShouldBeOfType<AIStandardAgentConfig>();
    }

    [Fact]
    public void Deserialize_StandardConfig_RoundTrips()
    {
        var original = new AIStandardAgentConfig
        {
            Instructions = "Test instructions",
            AllowedToolIds = ["tool-a"],
            AllowedToolScopeIds = ["scope-b"],
        };

        var json = AIAgentConfigSerializer.Serialize(original);
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Standard, json) as AIStandardAgentConfig;

        result.ShouldNotBeNull();
        result.Instructions.ShouldBe("Test instructions");
        result.AllowedToolIds.ShouldContain("tool-a");
        result.AllowedToolScopeIds.ShouldContain("scope-b");
    }

    [Fact]
    public void Deserialize_OrchestratedConfig_RoundTrips()
    {
        var original = new AIOrchestratedAgentConfig
        {
            Graph = new AIOrchestrationGraph
            {
                Nodes =
                [
                    new AIOrchestrationNode { Id = "n1", Type = AIOrchestrationNodeType.Agent, Label = "Node 1" },
                ],
                Edges =
                [
                    new AIOrchestrationEdge { Id = "e1", SourceNodeId = "n1", TargetNodeId = "n2" },
                ],
            },
        };

        var json = AIAgentConfigSerializer.Serialize(original);
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Orchestrated, json) as AIOrchestratedAgentConfig;

        result.ShouldNotBeNull();
        result.Graph.ShouldNotBeNull();
        result.Graph.Nodes.Count.ShouldBe(1);
        result.Graph.Nodes[0].Id.ShouldBe("n1");
        result.Graph.Edges.Count.ShouldBe(1);
        result.Graph.Edges[0].SourceNodeId.ShouldBe("n1");
    }

    #endregion
}
