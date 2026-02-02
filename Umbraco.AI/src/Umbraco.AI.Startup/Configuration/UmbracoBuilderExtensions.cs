using Umbraco.AI.Core.Providers;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Provides extension methods for configuring Umbraco AI services on the Umbraco builder.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco AI services to the Umbraco builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IUmbracoBuilder AddUmbracoAI(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(AIProviderCollection)))
            return builder;

        builder.AddUmbracoAiCore();
        builder.AddUmbracoAiPersistence();
        builder.AddUmbracoAiWeb();

        return builder;
    }
}