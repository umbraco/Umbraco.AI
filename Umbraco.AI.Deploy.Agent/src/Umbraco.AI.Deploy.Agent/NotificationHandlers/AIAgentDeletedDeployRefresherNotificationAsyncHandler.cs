using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Agent.NotificationHandlers;

internal sealed class AIAgentDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIAgent, AIAgentDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIAgentConstants.UdiEntityType.Agent)
{
}
