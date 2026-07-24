using Umbraco.AI.Agent.Conversations.Extensions;
using Umbraco.AI.Agent.Conversations.Persistence.Configuration;
using Umbraco.AI.Agent.Conversations.Web.Configuration;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Configuration;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Configuration;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Copilot.Workspace.Extensions;

/// <summary>
/// Extension methods for adding all Umbraco.AI.Agent.Copilot.Workspace services (the Workspace surface/UI
/// plus its Conversations persistence backend).
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds all Copilot Workspace services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAICopilotWorkspace(this IUmbracoBuilder builder)
    {
        // Conversations backend (reusable persistence sub-packages).
        builder.AddUmbracoAIConversationsCore();
        builder.AddUmbracoAIConversationsPersistence();
        builder.AddUmbracoAIConversationsWeb();

        // Copilot Workspace surface + web layer.
        builder.AddUmbracoAICopilotWorkspaceCore();
        builder.AddUmbracoAICopilotWorkspaceWeb();

        return builder;
    }
}
