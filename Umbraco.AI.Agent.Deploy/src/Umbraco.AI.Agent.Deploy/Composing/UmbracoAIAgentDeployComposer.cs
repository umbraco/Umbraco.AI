using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.AI.Agent.Deploy.NotificationHandlers;
using Umbraco.AI.Deploy.Composing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Infrastructure;

namespace Umbraco.AI.Agent.Deploy.Composing;

/// <summary>
/// Composer for the Umbraco AI Agent Deploy package, responsible for registering UDI types and notification handlers related to AI agents in the context of Umbraco Deploy.
/// </summary>
[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIAgentComposer))]
public class UmbracoAIAgentDeployComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Register component for UDI type registration
        builder.Components()
            .Append<UmbracoAIAgentDeployComponent>();

        // Disk entity type is serialized to UDA files
        builder.Services.Configure<DeployEntityTypesOptions>(options => options
            .AddDiskEntityType(UmbracoAIAgentConstants.UdiEntityType.Agent));

        // Register agent notification handlers
        builder.AddNotificationAsyncHandler<AIAgentSavedNotification, AIAgentSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIAgentDeletedNotification, AIAgentDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
