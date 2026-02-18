using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

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

        // Register UDI types
        RegisterUdiTypes();

        // Register notification handlers for automatic artifact management
        builder.AddNotificationAsyncHandler<AIContextSavedNotification, AIContextSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIContextDeletedNotification, AIContextDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIConnectionSavedNotification, AIConnectionSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIConnectionDeletedNotification, AIConnectionDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIProfileSavedNotification, AIProfileSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AIProfileDeletedNotification, AIProfileDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AISettingsSavedNotification, AISettingsSavedDeployRefresherNotificationAsyncHandler>();
        // Note: Settings is a singleton and cannot be deleted
    }

    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Settings, UdiType.GuidUdi);
    }
}
