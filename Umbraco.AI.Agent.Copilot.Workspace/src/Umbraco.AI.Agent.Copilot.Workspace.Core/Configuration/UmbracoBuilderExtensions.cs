using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Configuration;

/// <summary>
/// Extension methods for registering Umbraco.AI.Agent.Copilot.Workspace core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Copilot Workspace core services. The <c>copilot-workspace</c> agent surface is
    /// auto-discovered via its <c>[AIAgentSurface]</c> attribute.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAICopilotWorkspaceCore(this IUmbracoBuilder builder)
    {
        // TODO (Phase 4): register the Workspace section + section-access policy (SectionAccessCopilotWorkspace).
        return builder;
    }
}
