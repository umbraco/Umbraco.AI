using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Versioning;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentVersionableEntityAdapterTests
{
    private readonly IAIVersionableEntityAdapter _adapter = new AIAgentVersionableEntityAdapter(
        Mock.Of<IAIAgentService>(),
        Mock.Of<IAIEntityVersionService>());

    [Fact]
    public void SnapshotRoundTrip_PreservesGuardrailIdsAndScope()
    {
        var agent = new AIAgent
        {
            Alias = "probe",
            Name = "Probe",
            ProfileId = Guid.NewGuid(),
            GuardrailIds = [Guid.NewGuid(), Guid.NewGuid()],
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["content"], EntityTypes = ["document"] }]
            },
            Config = new AIStandardAgentConfig()
        };

        var json = _adapter.CreateSnapshot(agent);
        var restored = (AIAgent)_adapter.RestoreFromSnapshot(json)!;

        restored.GuardrailIds.ShouldBe(agent.GuardrailIds);
        restored.Scope.ShouldNotBeNull();
        restored.Scope!.AllowRules.Count.ShouldBe(1);
        restored.Scope.AllowRules[0].Sections.ShouldBe(new[] { "content" });
        restored.Scope.AllowRules[0].EntityTypes.ShouldBe(new[] { "document" });
    }

    [Fact]
    public void SnapshotRoundTrip_WithNoGuardrailsOrScope_RestoresEmptyAndNull()
    {
        var agent = new AIAgent
        {
            Alias = "probe",
            Name = "Probe",
            ProfileId = Guid.NewGuid(),
            Config = new AIStandardAgentConfig()
        };

        var json = _adapter.CreateSnapshot(agent);
        var restored = (AIAgent)_adapter.RestoreFromSnapshot(json)!;

        restored.GuardrailIds.ShouldBeEmpty();
        restored.Scope.ShouldBeNull();
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesOrchestratedAgentTypeAndConfig()
    {
        var agent = new AIAgent
        {
            Alias = "probe",
            Name = "Probe",
            ProfileId = Guid.NewGuid(),
            AgentType = AIAgentType.Orchestrated,
            Config = new AIOrchestratedAgentConfig { WorkflowId = "wf-1" }
        };

        var json = _adapter.CreateSnapshot(agent);

        // AgentType is serialized as a string (JsonStringEnumConverter is registered globally).
        json.ShouldContain("\"agentType\":\"Orchestrated\"");

        var restored = (AIAgent)_adapter.RestoreFromSnapshot(json)!;

        restored.AgentType.ShouldBe(AIAgentType.Orchestrated);
        var config = restored.Config.ShouldBeOfType<AIOrchestratedAgentConfig>();
        config.WorkflowId.ShouldBe("wf-1");
    }

    [Fact]
    public void CompareVersions_ReportsGuardrailAndScopeChanges()
    {
        var guardrailId = Guid.NewGuid();
        var from = new AIAgent
        {
            Alias = "probe",
            Name = "Probe",
            ProfileId = Guid.NewGuid(),
            Config = new AIStandardAgentConfig()
        };
        var to = new AIAgent
        {
            Alias = "probe",
            Name = "Probe",
            ProfileId = from.ProfileId,
            GuardrailIds = [guardrailId],
            Scope = new AIAgentScope { AllowRules = [new AIAgentScopeRule { Sections = ["content"] }] },
            Config = new AIStandardAgentConfig()
        };

        var changes = _adapter.CompareVersions(from, to);

        changes.ShouldContain(c => c.Path == "GuardrailIds");
        changes.ShouldContain(c => c.Path == "Scope");
    }
}
