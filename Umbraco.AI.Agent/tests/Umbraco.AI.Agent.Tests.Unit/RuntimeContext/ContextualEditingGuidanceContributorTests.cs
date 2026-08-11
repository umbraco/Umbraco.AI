using Shouldly;
using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.RuntimeContext;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.RuntimeContext;

public class ContextualEditingGuidanceContributorTests
{
    private sealed class TestSurface : IAIAgentSurface
    {
        public required string Id { get; init; }
        public string Icon => "icon-chat";
        public IReadOnlyList<string> SupportedScopeDimensions => [];
        public bool RestrictsDestructiveBackendTools { get; init; }
    }

    private static ContextualEditingGuidanceContributor CreateContributor(params IAIAgentSurface[] surfaces)
        => new(new AIAgentSurfaceCollection(() => surfaces));

    private static AIRuntimeContext ContextWithSurface(string? surfaceId)
    {
        var context = new AIRuntimeContext([]);
        if (surfaceId is not null)
        {
            context.SetValue(Constants.ContextKeys.Surface, surfaceId);
        }
        return context;
    }

    [Fact]
    public void Contribute_RestrictedSurface_AddsGuidanceSystemMessage()
    {
        var contributor = CreateContributor(new TestSurface { Id = "copilot", RestrictsDestructiveBackendTools = true });
        var context = ContextWithSurface("copilot");

        contributor.Contribute(context);

        context.SystemMessageParts.ShouldHaveSingleItem();
        context.SystemMessageParts[0].ShouldContain("only make changes to this one item");
    }

    [Fact]
    public void Contribute_UnrestrictedSurface_AddsNothing()
    {
        var contributor = CreateContributor(new TestSurface { Id = "workspace", RestrictsDestructiveBackendTools = false });
        var context = ContextWithSurface("workspace");

        contributor.Contribute(context);

        context.SystemMessageParts.ShouldBeEmpty();
    }

    [Fact]
    public void Contribute_NoSurfaceContext_AddsNothing()
    {
        var contributor = CreateContributor(new TestSurface { Id = "copilot", RestrictsDestructiveBackendTools = true });
        var context = ContextWithSurface(null);

        contributor.Contribute(context);

        context.SystemMessageParts.ShouldBeEmpty();
    }

    [Fact]
    public void Contribute_UnknownSurface_AddsNothing()
    {
        var contributor = CreateContributor(new TestSurface { Id = "copilot", RestrictsDestructiveBackendTools = true });
        var context = ContextWithSurface("something-not-registered");

        contributor.Contribute(context);

        context.SystemMessageParts.ShouldBeEmpty();
    }
}
