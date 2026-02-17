using Umbraco.AI.Deploy.Composing;
using Umbraco.AI.Deploy.Prompt.NotificationHandlers;
using Umbraco.AI.Prompt.Startup.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Deploy.Prompt.Composing;

[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIPromptComposer))]
public class UmbracoAIDeployPromptComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register UDI type
        UdiParser.RegisterUdiType(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi);

        // Register notification handlers
        builder.AddNotificationAsyncHandler<AIPromptSavedNotification, AIPromptSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIPromptDeletedNotification, AIPromptDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
