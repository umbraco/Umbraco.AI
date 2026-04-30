using System.Text.Json;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.InlineAgents;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.InlineAgents;

public class AIInlineAgentBuilderTests
{
    [Fact]
    public void Build_WithAlias_CreatesStandardAgent()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("alias");
    }

    [Fact]
    public void Build_WithBothInstructionsAndWorkflow_Throws()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
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
        var builder1 = new AIInlineAgentBuilder();
        builder1.WithAlias("deterministic-test");

        var builder2 = new AIInlineAgentBuilder();
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
        var builder1 = new AIInlineAgentBuilder();
        builder1.WithAlias("agent-alpha");

        var builder2 = new AIInlineAgentBuilder();
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
        var builder = new AIInlineAgentBuilder();

        // Assert
        builder.UseAllTools.ShouldBeFalse();
    }

    [Fact]
    public void WithAllTools_SetsFlag()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
        builder.WithAllTools();

        // Assert
        builder.UseAllTools.ShouldBeTrue();
    }

    [Fact]
    public void WithChatOptions_StoresOptions()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
        var options = new ChatOptions { Temperature = 0.5f, MaxOutputTokens = 100 };

        // Act
        builder.WithAlias("test-agent").WithChatOptions(options);

        // Assert
        builder.ChatOptions.ShouldBe(options);
    }

    [Fact]
    public void ChatOptions_DefaultsToNull()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();

        // Assert
        builder.ChatOptions.ShouldBeNull();
    }

    [Fact]
    public void Build_WithContexts_ById_WritesToConfigContextIds_Additive()
    {
        // Arrange
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var builder = new AIInlineAgentBuilder();
        builder.WithAlias("test-agent").WithContexts(a, b);

        // Act
        var agent = builder.Build();

        // Assert — WithContexts is additive: contexts flow via agent.Config.ContextIds so
        // AgentContextResolver emits them alongside the profile's contexts.
        var config = (AIStandardAgentConfig)agent.Config!;
        config.ContextIds.ShouldBe([a, b]);
    }

    [Fact]
    public void WithContexts_ByAlias_ExposesAdditionalAliases()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
        builder.WithAlias("test-agent").WithContexts("brand", "guidelines");

        // Assert
        builder.AdditionalContextAliases.ShouldBe(["brand", "guidelines"]);
        builder.ContextAliases.ShouldBeNull();
    }

    [Fact]
    public void WithContexts_ByAlias_ThenSetResolved_PopulatesConfig()
    {
        // Arrange
        var resolvedId = Guid.NewGuid();
        var builder = new AIInlineAgentBuilder();
        builder.WithAlias("test-agent").WithContexts("brand");

        // Act
        builder.SetResolvedAdditionalContextIds([resolvedId]);
        var agent = builder.Build();

        // Assert
        var config = (AIStandardAgentConfig)agent.Config!;
        config.ContextIds.ShouldBe([resolvedId]);
    }

    [Fact]
    public void SetContexts_ById_ExposesReplaceContextIds()
    {
        // Arrange
        var id = Guid.NewGuid();
        var builder = new AIInlineAgentBuilder();
        builder.WithAlias("test-agent").SetContexts(id);

        // Assert — SetContexts is replace: carried on the builder and emitted as a runtime override
        // key; does not touch agent.Config.ContextIds.
        builder.ContextIds.ShouldBe([id]);
        var agent = builder.Build();
        ((AIStandardAgentConfig)agent.Config!).ContextIds.ShouldBeEmpty();
    }

    [Fact]
    public void Build_WithoutContexts_HasEmptyContextIds()
    {
        // Arrange
        var builder = new AIInlineAgentBuilder();
        builder.WithAlias("test-agent");

        // Act
        var agent = builder.Build();

        // Assert
        var config = (AIStandardAgentConfig)agent.Config!;
        config.ContextIds.ShouldBeEmpty();
    }
}
