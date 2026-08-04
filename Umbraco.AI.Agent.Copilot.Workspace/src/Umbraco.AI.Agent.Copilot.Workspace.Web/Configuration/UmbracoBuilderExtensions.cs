using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Authorization;
using Umbraco.AI.Extensions;
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

        // Register the single product OpenAPI document. The Workspace stream/file controllers bind to it
        // via a compile-time [MapToApi]; the host-agnostic Conversations CRUD controllers are bound to it
        // (and gated behind the section policy) by the convention below (house one-doc-per-product).
        builder.WithUmbracoAIManagementApi(
            CopilotWorkspaceConstants.ManagementApi.ApiName,
            CopilotWorkspaceConstants.ManagementApi.ApiTitle,
            "Describes the Umbraco AI Copilot Workspace Management API for conversations, projects, and " +
            "persisted streaming, available when authenticated as a backoffice user with Copilot Workspace access.");

        // Bind the reusable Conversations/Projects controllers into this product's document + section
        // policy at runtime, so the Conversations web assembly carries no Copilot Workspace dependency.
        builder.Services.Configure<MvcOptions>(options =>
            options.Conventions.Add(new CopilotWorkspaceConversationsApiConvention()));

        // Stream + file controllers are auto-discovered (their DI dependencies — conversation/project
        // services, the ConversationChatHistoryProvider, and the IAIFileStore — are registered by the
        // Conversations core and Agent layers).
        return builder;
    }
}
