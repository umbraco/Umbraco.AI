using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agui.Streaming;

namespace Umbraco.AI.Agui.Configuration;

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
    public static IServiceCollection AddAgui(this IServiceCollection services)
    {
        return services.AddAgui(_ => { });
    }

    /// <summary>
    /// Adds AG-UI services to the service collection with options configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The options configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgui(
        this IServiceCollection services,
        Action<AguiStreamOptions> configureOptions)
    {
        var options = new AguiStreamOptions();
        configureOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<AguiEventSerializer>();

        return services;
    }
}
