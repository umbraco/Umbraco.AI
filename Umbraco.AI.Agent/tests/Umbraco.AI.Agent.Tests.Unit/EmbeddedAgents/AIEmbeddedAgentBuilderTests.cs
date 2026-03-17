using System.Text.Json;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.EmbeddedAgents;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.EmbeddedAgents;

public class AIEmbeddedAgentBuilderTests
{
    [Fact]
    public void Build_WithAlias_CreatesStandardAgent()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder.WithAlias("test-agent");

        // Act
        var agent = builder.Build();

        // Assert
        agent.ShouldNotBeNull();
        agent.Alias.ShouldBe("test-agent");
        agent.Name.ShouldBe("test-agent"); // Defaults to alias
        agent.AgentType.ShouldBe(AIAgentType.Standard);
        agent.IsActive.ShouldBeTrue();
        agent.SurfaceIds.ShouldBeEmpty();
        agent.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Build_WithNameAndDescription_SetsProperties()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-agent")
            .WithName("Test Agent")
            .WithDescription("A test description");

        // Act
        var agent = builder.Build();

        // Assert
        agent.Name.ShouldBe("Test Agent");
        agent.Description.ShouldBe("A test description");
    }

    [Fact]
    public void Build_WithProfile_SetsProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var builder = new AIEmbeddedAgentBuilder();
        builder.WithAlias("test-agent").WithProfile(profileId);

        // Act
        var agent = builder.Build();

        // Assert
        agent.ProfileId.ShouldBe(profileId);
    }

    [Fact]
    public void Build_WithInstructions_CreatesStandardConfig()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-agent")
            .WithInstructions("You are a helpful assistant.");

        // Act
        var agent = builder.Build();

        // Assert
        agent.AgentType.ShouldBe(AIAgentType.Standard);
        agent.Config.ShouldBeOfType<AIStandardAgentConfig>();
        var config = (AIStandardAgentConfig)agent.Config!;
        config.Instructions.ShouldBe("You are a helpful assistant.");
    }

    [Fact]
    public void Build_WithToolsAndScopes_SetsAllowedIds()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-agent")
            .WithTools("tool-a", "tool-b")
            .WithToolScopes("scope-x");

        // Act
        var agent = builder.Build();

        // Assert
        var config = (AIStandardAgentConfig)agent.Config!;
        config.AllowedToolIds.ShouldContain("tool-a");
        config.AllowedToolIds.ShouldContain("tool-b");
        config.AllowedToolScopeIds.ShouldContain("scope-x");
    }

    [Fact]
    public void Build_WithWorkflow_CreatesOrchestratedAgent()
    {
        // Arrange
        var settings = JsonDocument.Parse("""{"key": "value"}""").RootElement;
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-pipeline")
            .WithWorkflow("sequential-pipeline", settings);

        // Act
        var agent = builder.Build();

        // Assert
        agent.AgentType.ShouldBe(AIAgentType.Orchestrated);
        agent.Config.ShouldBeOfType<AIOrchestratedAgentConfig>();
        var config = (AIOrchestratedAgentConfig)agent.Config!;
        config.WorkflowId.ShouldBe("sequential-pipeline");
        config.Settings.ShouldNotBeNull();
    }

    [Fact]
    public void Build_WithGuardrails_SetsGuardrailIds()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-agent")
            .WithGuardrails(guardrailId);

        // Act
        var agent = builder.Build();

        // Assert
        agent.GuardrailIds.ShouldContain(guardrailId);
    }

    [Fact]
    public void Build_WithoutAlias_Throws()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("alias");
    }

    [Fact]
    public void Build_WithBothInstructionsAndWorkflow_Throws()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder
            .WithAlias("test-agent")
            .WithInstructions("Some instructions")
            .WithWorkflow("some-workflow");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Cannot configure both");
    }

    [Fact]
    public void Build_SameAlias_ProducesSameId()
    {
        // Arrange
        var builder1 = new AIEmbeddedAgentBuilder();
        builder1.WithAlias("deterministic-test");

        var builder2 = new AIEmbeddedAgentBuilder();
        builder2.WithAlias("deterministic-test");

        // Act
        var agent1 = builder1.Build();
        var agent2 = builder2.Build();

        // Assert
        agent1.Id.ShouldBe(agent2.Id);
    }

    [Fact]
    public void Build_DifferentAliases_ProduceDifferentIds()
    {
        // Arrange
        var builder1 = new AIEmbeddedAgentBuilder();
        builder1.WithAlias("agent-alpha");

        var builder2 = new AIEmbeddedAgentBuilder();
        builder2.WithAlias("agent-beta");

        // Act
        var agent1 = builder1.Build();
        var agent2 = builder2.Build();

        // Assert
        agent1.Id.ShouldNotBe(agent2.Id);
    }

    [Fact]
    public void UseAllTools_DefaultsFalse()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();

        // Assert
        builder.UseAllTools.ShouldBeFalse();
    }

    [Fact]
    public void WithAllTools_SetsFlag()
    {
        // Arrange
        var builder = new AIEmbeddedAgentBuilder();
        builder.WithAllTools();

        // Assert
        builder.UseAllTools.ShouldBeTrue();
    }
}
