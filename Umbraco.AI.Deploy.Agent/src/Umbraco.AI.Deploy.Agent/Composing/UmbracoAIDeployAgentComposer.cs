using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.AI.Deploy.Agent.NotificationHandlers;
using Umbraco.AI.Deploy.Composing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Deploy.Agent.Composing;

/// <summary>
/// Composer for the Umbraco AI Deploy Agent package, responsible for registering UDI types and notification handlers related to AI agents in the context of Umbraco Deploy.
/// </summary>
[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIAgentComposer))]
public class UmbracoAIDeployAgentComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Register component for UDI and disk entity type registration
        builder.Components()
            .Append<UmbracoAIDeployAgentComponent>();

        // Register notification handlers
        builder.AddNotificationAsyncHandler<AIAgentSavedNotification, AIAgentSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIAgentDeletedNotification, AIAgentDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
