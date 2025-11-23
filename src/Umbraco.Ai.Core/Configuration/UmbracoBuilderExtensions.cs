using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
//using Umbraco.Ai.Core.Services;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddUmbracoAi(this IUmbracoBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Config;

        // Prevent multiple registrations
        if (services.Any(x => x.ServiceType == typeof(IAiRegistry)))
            return builder;

        // Bind AiOptions from "Umbraco:Ai" section
        services.Configure<AiOptions>(config.GetSection("Umbraco:Ai"));

        // Scan and register providers
        RegisterProviders(services);

        // Registry
        services.AddSingleton<IAiRegistry, AiRegistry>();

        // High-level services
        //services.AddSingleton<IAiChatService, AiChatService>();
        // TODO: services.AddSingleton<IAiEmbeddingService, AiEmbeddingService>();
        // TODO: services.AddSingleton<IAiToolService, AiToolService>();

        return builder;
    }

    private static void RegisterProviders(IServiceCollection services)
    {
        // Get all assemblies in the current domain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // Skip system assemblies for performance
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true ||
                assembly.FullName?.StartsWith("netstandard") == true)
            {
                continue;
            }

            try
            {
                // Find all types decorated with [AiProvider]
                var providerTypes = assembly.GetTypes()
                    .Where(type =>
                        !type.IsAbstract &&
                        !type.IsInterface &&
                        type.GetCustomAttribute<AiProviderAttribute>() != null &&
                        typeof(IAiProvider).IsAssignableFrom(type))
                    .ToList();

                // Register each provider as singleton
                foreach (var providerType in providerTypes)
                {
                    services.AddSingleton(typeof(IAiProvider), providerType);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that fail to load types
                continue;
            }
        }
    }
}