using Umbraco.AI.Core.Settings;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

/// <summary>
/// Handles AI settings saved notifications and writes deployment artifacts.
/// </summary>
internal sealed class AISettingsSavedDeployRefresherNotificationAsyncHandler
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AISettings, AISettingsSavedNotification>
{
    public AISettingsSavedDeployRefresherNotificationAsyncHandler(
        IServiceConnectorFactory serviceConnectorFactory,
        IDiskEntityService diskEntityService,
        ISignatureService signatureService)
        : base(serviceConnectorFactory, diskEntityService, signatureService, UmbracoAIConstants.UdiEntityType.Settings)
    {
    }

    protected override object GetEntityId(AISettings entity) => entity.Id;
}
