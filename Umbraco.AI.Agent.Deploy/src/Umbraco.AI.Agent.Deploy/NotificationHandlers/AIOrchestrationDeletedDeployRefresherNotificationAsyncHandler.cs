using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Agent.Deploy.NotificationHandlers;

internal sealed class AIOrchestrationDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIOrchestration, AIOrchestrationDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIAgentConstants.UdiEntityType.Orchestration);
