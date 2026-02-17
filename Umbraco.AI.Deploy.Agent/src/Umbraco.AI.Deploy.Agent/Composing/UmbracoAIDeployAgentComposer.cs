using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Composing;
using Umbraco.AI.Deploy.Agent.NotificationHandlers;
using Umbraco.AI.Deploy.Composing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Deploy.Agent.Composing;

[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIAgentComposer))]
public class UmbracoAIDeployAgentComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register UDI type
        UdiParser.RegisterUdiType(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi);

        // Register notification handlers
        builder.AddNotificationAsyncHandler<AIAgentSavedNotification, AIAgentSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIAgentDeletedNotification, AIAgentDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
