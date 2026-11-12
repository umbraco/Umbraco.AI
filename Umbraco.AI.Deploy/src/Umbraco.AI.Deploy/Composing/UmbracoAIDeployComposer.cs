using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Infrastructure;

namespace Umbraco.AI.Deploy.Composing;

/// <summary>
/// Composer for Umbraco AI Deploy integration, responsible for registering services, UDI types, and notification handlers to manage AI-related artifacts during deployment.
/// </summary>
public class UmbracoAIDeployComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Configuration
        builder.Services.AddOptions<UmbracoAIDeploySettings>()
            .Bind(builder.Config.GetSection("Umbraco:AI:Deploy"));

        builder.Services.AddSingleton<UmbracoAIDeploySettingsAccessor>();

        // Register component for UDI type registration
        builder.Components()
            .Append<UmbracoAIDeployComponent>();

        // Disk entity types are serialized to UDA files
        builder.Services.Configure<DeployEntityTypesOptions>(options => options
            .AddDiskEntityType(UmbracoAIConstants.UdiEntityType.Context)
            .AddDiskEntityType(UmbracoAIConstants.UdiEntityType.Connection)
            .AddDiskEntityType(UmbracoAIConstants.UdiEntityType.Guardrail)
            .AddDiskEntityType(UmbracoAIConstants.UdiEntityType.Profile)
            .AddDiskEntityType(UmbracoAIConstants.UdiEntityType.Settings));

        // Register notification handlers for automatic artifact management
        builder.AddNotificationAsyncHandler<AIContextSavedNotification, AIContextSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIContextDeletedNotification, AIContextDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIConnectionSavedNotification, AIConnectionSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIConnectionDeletedNotification, AIConnectionDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIProfileSavedNotification, AIProfileSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIProfileDeletedNotification, AIProfileDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIGuardrailSavedNotification, AIGuardrailSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIGuardrailDeletedNotification, AIGuardrailDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AISettingsSavedNotification, AISettingsSavedDeployRefresherNotificationAsyncHandler>();
        // Note: Settings is a singleton and cannot be deleted
    }
}
