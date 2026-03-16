using Umbraco.AI.Core.Guardrails;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIGuardrailSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIGuardrail, AIGuardrailSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Guardrail);
