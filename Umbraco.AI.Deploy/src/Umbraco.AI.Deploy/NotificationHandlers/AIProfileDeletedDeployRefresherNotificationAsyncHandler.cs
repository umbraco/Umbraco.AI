using Umbraco.AI.Core.Profiles;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIProfileDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIProfile, AIProfileDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Profile);
