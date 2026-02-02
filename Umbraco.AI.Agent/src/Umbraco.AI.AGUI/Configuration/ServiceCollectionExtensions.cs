using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.AGUI.Streaming;

namespace Umbraco.AI.AGUI.Configuration;

/// <summary>
/// Extension methods for configuring AG-UI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AG-UI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAGUI(this IServiceCollection services)
    {
        return services.AddAGUI(_ => { });
    }

    /// <summary>
    /// Adds AG-UI services to the service collection with options configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The options configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAGUI(
        this IServiceCollection services,
        Action<AGUIStreamOptions> configureOptions)
    {
        var options = new AGUIStreamOptions();
        configureOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<AGUIEventSerializer>();

        return services;
    }
}
