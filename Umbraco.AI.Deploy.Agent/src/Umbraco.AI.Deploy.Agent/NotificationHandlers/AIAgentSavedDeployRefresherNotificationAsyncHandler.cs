using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Agent.NotificationHandlers;

internal sealed class AIAgentSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIAgent, AIAgentSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIAgentConstants.UdiEntityType.Agent)
{
    protected override object GetEntityId(AIAgent entity) => entity.Id;
}
