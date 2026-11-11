using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Deploy.Composing;
using Umbraco.AI.Prompt.Deploy.NotificationHandlers;
using Umbraco.AI.Prompt.Startup.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Infrastructure;

namespace Umbraco.AI.Prompt.Deploy.Composing;

/// <summary>
/// Composer for Umbraco AI Prompt Deploy package, responsible for registering UDI types and notification handlers related to AI Prompts in the deployment process.
/// </summary>
[ComposeAfter(typeof(UmbracoAIDeployComposer))]
[ComposeAfter(typeof(UmbracoAIPromptComposer))]
public class UmbracoAIPromptDeployComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Register component for UDI type registration
        builder.Components()
            .Append<UmbracoAIPromptDeployComponent>();

        // Disk entity type is serialized to UDA files
        builder.Services.Configure<DeployEntityTypesOptions>(options => options
            .AddDiskEntityType(UmbracoAIPromptConstants.UdiEntityType.Prompt));

        // Register notification handlers
        builder.AddNotificationAsyncHandler<AIPromptSavedNotification, AIPromptSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIPromptDeletedNotification, AIPromptDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
