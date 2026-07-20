using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Conversations.Web.Configuration;

/// <summary>
/// Extension methods for registering the Umbraco.AI.Agent.Conversations management API.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations management API (conversation + project CRUD controllers, mapping, models).
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsWeb(this IUmbracoBuilder builder)
    {
        // TODO (Phase 4): register controllers/mapping; controllers bind to the
        // ai-copilot-workspace-management OpenAPI document registered by the Workspace composer.
        return builder;
    }
}
