using System.Text.Json;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
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
            WorkflowId = "sequential-pipeline",
            Settings = JsonDocument.Parse("{\"agents\":[\"a1\",\"a2\"]}").RootElement,
        };

        var result = AIAgentConfigSerializer.Serialize(config);

        result.ShouldNotBeNull();
        result.ShouldContain("\"workflowId\":\"sequential-pipeline\"");
        result.ShouldContain("\"settings\"");
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
            WorkflowId = "round-robin",
            Settings = JsonDocument.Parse("{\"maxRounds\":5}").RootElement,
        };

        var json = AIAgentConfigSerializer.Serialize(original);
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Orchestrated, json) as AIOrchestratedAgentConfig;

        result.ShouldNotBeNull();
        result.WorkflowId.ShouldBe("round-robin");
        result.Settings.ShouldNotBeNull();
        result.Settings.Value.GetProperty("maxRounds").GetInt32().ShouldBe(5);
    }

    [Fact]
    public void Deserialize_OldGraphData_IgnoresUnknownProperties()
    {
        // Old data with "graph" field should deserialize without errors
        var oldJson = """{"graph":{"nodes":[],"edges":[]}}""";
        var result = AIAgentConfigSerializer.Deserialize(AIAgentType.Orchestrated, oldJson) as AIOrchestratedAgentConfig;

        result.ShouldNotBeNull();
        result.WorkflowId.ShouldBeNull();
        result.Settings.ShouldBeNull();
    }

    #endregion
}
