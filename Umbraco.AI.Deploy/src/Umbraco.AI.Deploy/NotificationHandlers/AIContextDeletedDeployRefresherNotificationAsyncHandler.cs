using Umbraco.AI.Core.Contexts;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIContextDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIContext, AIContextDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Context);
