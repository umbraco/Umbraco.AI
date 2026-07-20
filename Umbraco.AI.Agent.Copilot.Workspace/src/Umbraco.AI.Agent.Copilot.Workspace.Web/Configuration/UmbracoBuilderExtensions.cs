using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Configuration;

/// <summary>
/// Extension methods for registering the Copilot Workspace web layer.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Copilot Workspace web layer (section-gated persisted stream endpoint + authenticated
    /// file endpoint) and registers the <c>ai-copilot-workspace-management</c> OpenAPI document.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAICopilotWorkspaceWeb(this IUmbracoBuilder builder)
    {
        // TODO (Phase 4): register the ai-copilot-workspace-management document + stream/file controllers.
        return builder;
    }
}
