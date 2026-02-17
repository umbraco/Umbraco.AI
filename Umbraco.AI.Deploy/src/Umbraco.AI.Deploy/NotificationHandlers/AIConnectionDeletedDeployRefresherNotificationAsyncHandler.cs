using Umbraco.AI.Core.Connections;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIConnectionDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIConnection, AIConnectionDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Connection)
{
}
