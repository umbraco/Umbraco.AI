// S3 - I can see why an agent was picked (audit metadata: AC4, AC5, AC7, AC8)
// Seam: the additional properties AIAgentService hands to IAIAgentFactory become the run's runtime
// context, and the tracker copies exactly the keys listed in LogKeys into AIAuditLog.Metadata.
// So each audit criterion is pinned as "value is set" plus "key is listed in LogKeys".
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agents.Selection;
using Xunit;
using static Umbraco.AI.Agent.Tests.Unit.Agents.Selection.AgentSelectionTestBuilders;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents.Selection;

public class AgentSelectionAuditMetadataTests
{
    private const string SelectorIdKey = "Umbraco.AI.Agent.SelectorId";
    private const string SelectionReasonKey = "Umbraco.AI.Agent.SelectionReason";

    private static readonly UmbracoAIAgent AgentB = CreateAgent(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), "agent-b");

    private static AgentServiceAuditHarness Run(AIAgentExecutionOptions options)
    {
        var harness = new AgentServiceAuditHarness(AgentB);
        harness.RunAsync(options, AgentB.Id).GetAwaiter().GetResult();
        return harness;
    }

    // ---- Happy path --------------------------------------------------------------------------

    // AC4 - Audit metadata: selector / AC5 - Audit metadata: reason
    public class GivenASelectionWithAReason
    {
        private readonly AgentServiceAuditHarness _harness = Run(new AIAgentExecutionOptions
        {
            Selection = new AIAgentSelectionResult(AgentB, "my-rule", "editing products"),
        });

        [Fact]
        public void SetsTheSelectorId() => _harness.AdditionalProperties![SelectorIdKey].ShouldBe("my-rule");

        [Fact]
        public void ListsTheSelectorIdInLogKeys() => _harness.LogKeys.ShouldContain(SelectorIdKey);

        [Fact]
        public void SetsTheSelectionReason() => _harness.AdditionalProperties![SelectionReasonKey].ShouldBe("editing products");

        [Fact]
        public void ListsTheSelectionReasonInLogKeys() => _harness.LogKeys.ShouldContain(SelectionReasonKey);
    }

    // ---- Sad path / edge ---------------------------------------------------------------------

    // AC7 - Null reason is omitted from audit
    public class GivenASelectionWithANullReason
    {
        private readonly AgentServiceAuditHarness _harness = Run(new AIAgentExecutionOptions
        {
            Selection = new AIAgentSelectionResult(AgentB, "my-rule", null),
        });

        [Fact]
        public void DoesNotListTheSelectionReasonInLogKeys() => _harness.LogKeys.ShouldNotContain(SelectionReasonKey);
    }

    // AC8 - Explicit runs have no selection metadata
    public class GivenAnExplicitRunWithNoSelection
    {
        private readonly AgentServiceAuditHarness _harness = Run(new AIAgentExecutionOptions());

        [Fact]
        public void DoesNotListTheSelectorIdInLogKeys() => _harness.LogKeys.ShouldNotContain(SelectorIdKey);

        [Fact]
        public void DoesNotListTheSelectionReasonInLogKeys() => _harness.LogKeys.ShouldNotContain(SelectionReasonKey);
    }
}
