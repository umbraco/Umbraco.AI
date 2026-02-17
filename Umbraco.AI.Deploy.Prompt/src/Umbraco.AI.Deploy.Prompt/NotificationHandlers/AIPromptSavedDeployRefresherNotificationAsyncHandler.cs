using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Prompt.NotificationHandlers;

internal sealed class AIPromptSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<AIPrompt, AIPromptSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        UmbracoAIPromptConstants.UdiEntityType.Prompt)
{
    protected override object GetEntityId(AIPrompt entity) => entity.Id;
}
