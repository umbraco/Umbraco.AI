using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Web.NotificationHandlers;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

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
        builder.Services.AddAuthorization(ConfigureAuthorization);

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

        // Reject saving a conversation whose named agent can't run in Workspace. The check lives here, not
        // in the host-agnostic Conversations layer, because only the host knows its surface.
        builder.AddNotificationAsyncHandler<AIConversationSavingNotification, CopilotWorkspaceConversationAgentSavingNotificationHandler>();

        // Stream + file controllers are auto-discovered (their DI dependencies — conversation/project
        // services, the ConversationChatHistoryProvider, and the IAIFileStore — are registered by the
        // Conversations core and Agent layers).
        return builder;
    }

    /// <summary>
    /// Registers the Copilot Workspace section policy. Mirrors how the CMS builds its own backoffice
    /// policies: both the OpenIddict validation scheme (bearer tokens) and the backoffice cookie scheme.
    /// </summary>
    internal static void ConfigureAuthorization(AuthorizationOptions options)
        => options.AddPolicy(CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace, policy =>
        {
            policy.AddAuthenticationSchemes(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType);
            policy.Requirements.Add(new CopilotWorkspaceSectionRequirement());
        });
}
