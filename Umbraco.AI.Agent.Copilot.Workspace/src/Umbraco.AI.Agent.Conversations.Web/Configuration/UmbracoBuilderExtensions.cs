using Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;
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
    /// The controllers are host-agnostic: the hosting product binds them to its own OpenAPI document and
    /// section-access policy via an application-model convention (see
    /// <see cref="ConversationsManagementControllerBase"/>).
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
