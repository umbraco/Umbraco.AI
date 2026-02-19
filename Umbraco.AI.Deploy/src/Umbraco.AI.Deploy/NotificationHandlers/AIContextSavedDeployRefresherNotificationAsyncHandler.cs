using Umbraco.AI.Core.Contexts;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

internal sealed class AIContextSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIContext, AIContextSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIConstants.UdiEntityType.Context);

