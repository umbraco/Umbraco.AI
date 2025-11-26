using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Persistence.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

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
    public static IUmbracoBuilder AddUmbracoAi(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiRegistry)))
            return builder;

        builder.AddUmbracoAiCore();
        builder.AddUmbracoAiPersistence();
        builder.AddUmbracoAiWeb();

        return builder;
    }
}