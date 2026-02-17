using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.NotificationHandlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Deploy.Composing;

public class UmbracoAIDeployComposer : IComposer
{
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
    }

    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi);
    }
}
