using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.AI.Agent.Deploy.NotificationHandlers;
using Umbraco.AI.Deploy.Composing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Deploy.Composing;

/// <summary>
/// Composer for the Umbraco AI Agent Deploy package, responsible for registering UDI types and notification handlers related to AI agents and orchestrations in the context of Umbraco Deploy.
/// </summary>
[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIAgentComposer))]
public class UmbracoAIAgentDeployComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Register component for UDI and disk entity type registration
        builder.Components()
            .Append<UmbracoAIAgentDeployComponent>();

        // Register agent notification handlers
        builder.AddNotificationAsyncHandler<AIAgentSavedNotification, AIAgentSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIAgentDeletedNotification, AIAgentDeletedDeployRefresherNotificationAsyncHandler>();

        // Register orchestration notification handlers
        builder.AddNotificationAsyncHandler<AIOrchestrationSavedNotification, AIOrchestrationSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIOrchestrationDeletedNotification, AIOrchestrationDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
