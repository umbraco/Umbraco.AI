using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Core.Settings;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI services registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    internal static IUmbracoBuilder AddUmbracoAiCore(this IUmbracoBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Config;

        // Prevent multiple registrations
        if (services.Any(x => x.ServiceType == typeof(IAiRegistry)))
            return builder;

        // Bind AiOptions from "Umbraco:Ai" section
        services.Configure<AiOptions>(config.GetSection("Umbraco:Ai"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
        services.AddSingleton<IAiSettingDefinitionBuilder, AiSettingDefinitionBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

        // Auto-discover providers using TypeLoader (uses Umbraco's cached, efficient type discovery)
        builder.AiProviders()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiProvider, AiProviderAttribute>(cache: true));

        // Initialize middleware collection builders (empty by default, consumers can add via Composers)
        // Use AiChatMiddleware() and AiEmbeddingMiddleware() extension methods to add middleware
        _ = builder.AiChatMiddleware();
        _ = builder.AiEmbeddingMiddleware();

        // Registry
        services.AddSingleton<IAiRegistry, AiRegistry>();

        // Settings resolution
        services.AddSingleton<IAiSettingsResolver, AiSettingsResolver>();

        // Connection system
        services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
        services.AddSingleton<IAiConnectionService, AiConnectionService>();

        // Profile resolution
        services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
        services.AddSingleton<IAiProfileService, AiProfileService>();

        // Client factories
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddSingleton<IAiEmbeddingGeneratorFactory, AiEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAiChatService, AiChatService>();
        services.AddSingleton<IAiEmbeddingService, AiEmbeddingService>();
        // TODO: services.AddSingleton<IAiToolService, AiToolService>();

        return builder;
    }
}