using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Agent.Deploy.NotificationHandlers;

internal sealed class AIOrchestrationSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIOrchestration, AIOrchestrationSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIAgentConstants.UdiEntityType.Orchestration);
