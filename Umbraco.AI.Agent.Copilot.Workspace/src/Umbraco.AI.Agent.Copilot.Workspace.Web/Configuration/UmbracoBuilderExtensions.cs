using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Authorization;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Configuration;

/// <summary>
/// Extension methods for registering the Copilot Workspace web layer.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Copilot Workspace web layer: the section-access authorization policy (which also gates
    /// the Conversations CRUD API — F-SEC), and (Phase 4) the section-gated persisted stream endpoint +
    /// authenticated file endpoint + the <c>ai-copilot-workspace-management</c> OpenAPI document.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAICopilotWorkspaceWeb(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, CopilotWorkspaceSectionAuthorizationHandler>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace, policy =>
            {
                policy.Requirements.Add(new CopilotWorkspaceSectionRequirement());
            });

        // TODO (Phase 4): register the ai-copilot-workspace-management document + stream/file controllers.
        return builder;
    }
}
