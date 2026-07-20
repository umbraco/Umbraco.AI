using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Conversations.Web.Configuration;

/// <summary>
/// Extension methods for registering the Umbraco.AI.Agent.Conversations management API.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations management API (conversation + project CRUD controllers, mapping, models).
    /// Controllers bind to the <c>ai-copilot-workspace-management</c> OpenAPI document registered by the
    /// Workspace composer.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsWeb(this IUmbracoBuilder builder)
    {
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<ConversationMapDefinition>()
            .Add<ProjectMapDefinition>();

        return builder;
    }
}
