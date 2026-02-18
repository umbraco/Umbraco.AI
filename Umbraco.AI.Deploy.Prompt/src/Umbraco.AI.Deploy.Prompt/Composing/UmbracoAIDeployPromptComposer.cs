using Umbraco.AI.Deploy.Composing;
using Umbraco.AI.Deploy.Prompt.NotificationHandlers;
using Umbraco.AI.Prompt.Startup.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Deploy.Prompt.Composing;

/// <summary>
/// Composer for Umbraco AI Deploy Prompt package, responsible for registering UDI types and notification handlers related to AI Prompts in the deployment process.
/// </summary>
[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIPromptComposer))]
public class UmbracoAIDeployPromptComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Register UDI type
        UdiParser.RegisterUdiType(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi);

        // Register notification handlers
        builder.AddNotificationAsyncHandler<AIPromptSavedNotification, AIPromptSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIPromptDeletedNotification, AIPromptDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
