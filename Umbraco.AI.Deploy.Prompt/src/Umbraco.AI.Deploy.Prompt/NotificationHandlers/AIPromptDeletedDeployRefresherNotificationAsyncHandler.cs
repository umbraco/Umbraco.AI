using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Prompt.NotificationHandlers;

internal sealed class AIPromptDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AIPrompt, AIPromptDeletedNotification>(
        diskEntityService,
        signatureService,
        UmbracoAIPromptConstants.UdiEntityType.Prompt);
