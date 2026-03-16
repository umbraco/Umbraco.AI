using Umbraco.AI.Core.Guardrails;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIGuardrailDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIGuardrail, AIGuardrailDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Guardrail);
