using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Versioning;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// First coverage of <see cref="AIAgentVersionableEntityAdapter"/>: snapshot, restore and compare,
/// with a focus on <see cref="AIAgent.StarterPrompts"/> since it is the newest field to travel
/// through this path.
/// </summary>
public class AIAgentVersionableEntityAdapterTests
{
    private readonly IAIVersionableEntityAdapter _adapter;

    public AIAgentVersionableEntityAdapterTests()
    {
        _adapter = new AIAgentVersionableEntityAdapter(
            Mock.Of<IAIAgentService>(),
            Mock.Of<IAIEntityVersionService>());
    }

    [Fact]
    public void SnapshotAndRestore_RoundTripsStarterPrompts()
    {
        // Arrange
        var agent = CreateAgent(
        [
            new AIStarterPrompt { Prompt = "First starter" },
            new AIStarterPrompt { Prompt = "Second starter" },
        ]);

        // Act
        var snapshotJson = _adapter.CreateSnapshot(agent);
        var restored = (AIAgent?)_adapter.RestoreFromSnapshot(snapshotJson);

        // Assert
        restored.ShouldNotBeNull();
        restored.StarterPrompts.Select(p => p.Prompt).ShouldBe(["First starter", "Second starter"]);
    }

    [Fact]
    public void SnapshotAndRestore_WithNoStarterPrompts_RestoresEmptyList()
    {
        // Arrange
        var agent = CreateAgent([]);

        // Act
        var snapshotJson = _adapter.CreateSnapshot(agent);
        var restored = (AIAgent?)_adapter.RestoreFromSnapshot(snapshotJson);

        // Assert
        restored.ShouldNotBeNull();
        restored.StarterPrompts.ShouldBeEmpty();
    }

    [Fact]
    public void CompareVersions_WithChangedStarterPrompts_ReportsAChange()
    {
        // Arrange
        var from = CreateAgent([new AIStarterPrompt { Prompt = "Old starter" }]);
        var to = CreateAgent([new AIStarterPrompt { Prompt = "New starter" }]);

        // Act
        var changes = _adapter.CompareVersions(from, to);

        // Assert
        changes.ShouldContain(c => c.Path == "StarterPrompts");
    }

    [Fact]
    public void CompareVersions_WithIdenticalStarterPrompts_ReportsNoStarterPromptsChange()
    {
        // Arrange
        var from = CreateAgent([new AIStarterPrompt { Prompt = "Same starter" }]);
        var to = CreateAgent([new AIStarterPrompt { Prompt = "Same starter" }]);

        // Act
        var changes = _adapter.CompareVersions(from, to);

        // Assert
        changes.ShouldNotContain(c => c.Path == "StarterPrompts");
    }

    [Fact]
    public void CompareVersions_WithReorderedStarterPrompts_ReportsAChange()
    {
        // Arrange — reordering is itself an authored change
        var from = CreateAgent(
        [
            new AIStarterPrompt { Prompt = "A" },
            new AIStarterPrompt { Prompt = "B" },
        ]);
        var to = CreateAgent(
        [
            new AIStarterPrompt { Prompt = "B" },
            new AIStarterPrompt { Prompt = "A" },
        ]);

        // Act
        var changes = _adapter.CompareVersions(from, to);

        // Assert
        changes.ShouldContain(c => c.Path == "StarterPrompts");
    }

    private static AIAgent CreateAgent(IReadOnlyList<AIStarterPrompt> starterPrompts)
        => new()
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            ProfileId = Guid.NewGuid(),
            StarterPrompts = starterPrompts,
        };
}
