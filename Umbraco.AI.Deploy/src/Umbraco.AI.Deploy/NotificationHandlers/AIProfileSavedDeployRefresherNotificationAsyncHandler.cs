using Umbraco.AI.Core.Profiles;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIProfileSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIProfile, AIProfileSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Profile);

