using Umbraco.Ai.Core.Registry;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddUmbracoAi(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiRegistry)))
            return builder;
        
        builder.AddUmbracoAiCore();
        builder.AddUmbracoAiWeb();
        
        return builder;
    }
}