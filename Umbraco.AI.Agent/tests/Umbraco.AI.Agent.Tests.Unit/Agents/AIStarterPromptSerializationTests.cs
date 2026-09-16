using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence.Agents;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Tests for <see cref="AIAgentEntityFactory"/>'s handling of <see cref="AIAgent.StarterPrompts"/>,
/// stored as a single JSON blob column, additive and null when empty.
/// </summary>
public class AIStarterPromptSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesOrderAndContent()
    {
        // Arrange
        var agent = CreateAgent(
        [
            new AIStarterPrompt { Prompt = "First starter" },
            new AIStarterPrompt { Prompt = "Second starter" },
            new AIStarterPrompt { Prompt = "Third starter" },
        ]);

        // Act
        var entity = AIAgentEntityFactory.BuildEntity(agent);
        var roundTripped = AIAgentEntityFactory.BuildDomain(entity);

        // Assert
        roundTripped.StarterPrompts.Count.ShouldBe(3);
        roundTripped.StarterPrompts.Select(p => p.Prompt).ShouldBe(
        [
            "First starter",
            "Second starter",
            "Third starter",
        ]);
    }

    [Fact]
    public void BuildEntity_WithNoStarterPrompts_StoresNullColumn()
    {
        // Arrange
        var agent = CreateAgent([]);

        // Act
        var entity = AIAgentEntityFactory.BuildEntity(agent);

        // Assert
        entity.StarterPrompts.ShouldBeNull();
    }

    [Fact]
    public void BuildDomain_WithNullColumn_ReturnsEmptyList()
    {
        // Arrange
        var entity = CreateEntity(starterPromptsJson: null);

        // Act
        var agent = AIAgentEntityFactory.BuildDomain(entity);

        // Assert
        agent.StarterPrompts.ShouldNotBeNull();
        agent.StarterPrompts.ShouldBeEmpty();
    }

    [Fact]
    public void BuildDomain_WithMalformedJson_ReturnsEmptyListRatherThanThrowing()
    {
        // Arrange
        var entity = CreateEntity(starterPromptsJson: "{ not valid json ]");

        // Act
        var agent = AIAgentEntityFactory.BuildDomain(entity);

        // Assert
        agent.StarterPrompts.ShouldNotBeNull();
        agent.StarterPrompts.ShouldBeEmpty();
    }

    private static AIAgent CreateAgent(IReadOnlyList<AIStarterPrompt> starterPrompts)
        => new()
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            StarterPrompts = starterPrompts,
        };

    private static AIAgentEntity CreateEntity(string? starterPromptsJson)
        => new()
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = (int)AIAgentType.Standard,
            StarterPrompts = starterPromptsJson,
        };
}
