using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Surfaces;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Verifies <see cref="AIAgentScopeValidator"/> availability semantics, with particular focus on
/// the broad/unscoped surface case (empty <see cref="IAIAgentSurface.SupportedScopeDimensions"/>,
/// e.g. the copilot-workspace surface) where dimension-based rules cannot meaningfully apply.
/// </summary>
public class AIAgentScopeValidatorTests
{
    private readonly AIAgentScopeValidator _sut = new();

    private static AIAgent AgentWithScope(AIAgentScope? scope) =>
        new() { Alias = "test-agent", Name = "Test Agent", Scope = scope };

    private static IAIAgentSurface Surface(params string[] dimensions)
    {
        var mock = new Mock<IAIAgentSurface>();
        mock.SetupGet(x => x.Id).Returns("test-surface");
        mock.SetupGet(x => x.Icon).Returns("icon-chat");
        mock.SetupGet(x => x.SupportedScopeDimensions).Returns(dimensions);
        return mock.Object;
    }

    private static AgentAvailabilityContext ContentDocument => new()
    {
        Section = "content",
        EntityType = "document",
    };

    [Fact]
    public void NullScope_ReturnsTrue()
    {
        var result = _sut.IsAgentAvailable(AgentWithScope(null), ContentDocument, Surface("section", "entityType"));

        result.ShouldBeTrue();
    }

    // --- Broad surface (empty dimensions) — the copilot-workspace case ---

    [Fact]
    public void EmptyDimensions_WithDenyRule_ReturnsTrue()
    {
        // A deny rule must NOT match vacuously on a dimensionless surface (regression guard for S5:
        // previously every deny rule matched → agent denied everywhere on the workspace surface).
        var agent = AgentWithScope(new AIAgentScope
        {
            DenyRules = [new AIAgentScopeRule { Sections = ["settings"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, Surface(/* no dimensions */));

        result.ShouldBeTrue();
    }

    [Fact]
    public void EmptyDimensions_WithAllowRule_ReturnsTrue()
    {
        var agent = AgentWithScope(new AIAgentScope
        {
            AllowRules = [new AIAgentScopeRule { Sections = ["content"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, Surface(/* no dimensions */));

        result.ShouldBeTrue();
    }

    [Fact]
    public void NullSurface_ReturnsTrue()
    {
        var agent = AgentWithScope(new AIAgentScope
        {
            DenyRules = [new AIAgentScopeRule { Sections = ["settings"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, surface: null);

        result.ShouldBeTrue();
    }

    // --- Scoped surface (section + entityType) — existing Copilot behaviour must be preserved ---

    [Fact]
    public void ScopedSurface_DenyRuleMatches_ReturnsFalse()
    {
        var agent = AgentWithScope(new AIAgentScope
        {
            DenyRules = [new AIAgentScopeRule { Sections = ["content"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, Surface("section", "entityType"));

        result.ShouldBeFalse();
    }

    [Fact]
    public void ScopedSurface_AllowRuleMatches_ReturnsTrue()
    {
        var agent = AgentWithScope(new AIAgentScope
        {
            AllowRules = [new AIAgentScopeRule { Sections = ["content"], EntityTypes = ["document"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, Surface("section", "entityType"));

        result.ShouldBeTrue();
    }

    [Fact]
    public void ScopedSurface_AllowRuleDoesNotMatch_ReturnsFalse()
    {
        var agent = AgentWithScope(new AIAgentScope
        {
            AllowRules = [new AIAgentScopeRule { Sections = ["media"] }],
        });

        var result = _sut.IsAgentAvailable(agent, ContentDocument, Surface("section", "entityType"));

        result.ShouldBeFalse();
    }
}
