using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.DevUI.Middleware;
using Umbraco.Ai.DevUI.Services;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.DevUI.Extensions;

/// <summary>
/// Extension methods for configuring DevUI in Umbraco.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds DevUI services and configuration to the Umbraco builder.
    /// Only registers DevUI if the application is running in Development mode.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoAiDevUI(this IUmbracoBuilder builder)
    {
        // Check if we're in Development mode before registering any services
        // We need to build a temporary service provider to check the environment
        using (var tempServiceProvider = builder.Services.BuildServiceProvider())
        {
            var hostEnvironment = tempServiceProvider.GetService<IHostEnvironment>();
            if (hostEnvironment?.IsDevelopment() != true)
            {
                var logger = tempServiceProvider.GetService<ILogger<IDevUIEntityDiscoveryService>>();
                logger?.LogInformation("DevUI registration skipped - application is not in Development mode");
                return builder;
            }
        }

        // Add Microsoft DevUI services
        builder.Services.AddDevUI();

        // Register services for OpenAI responses and conversations (required for DevUI)
        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();

        // Register DevUI entity discovery service
        builder.Services.AddSingleton<IDevUIEntityDiscoveryService, DevUIEntityDiscoveryService>();

        // Register a keyed service to resolve AIAgent instances by alias for DevUI
        builder.Services.AddKeyedSingleton<AIAgent>(KeyedService.AnyKey, (sp, key) =>
        {
            var keyAsStr = key as string;
            if (string.IsNullOrEmpty(keyAsStr))
            {
                return null!;
            }

            var agentService = sp.GetRequiredService<IAiAgentService>();
            var agents = agentService.GetAgentsAsync().GetAwaiter().GetResult();
            var agent = agents.FirstOrDefault(a => a.Alias.Equals(keyAsStr, StringComparison.Ordinal));
            if (agent is not null)
            {
                var agentFactory = sp.GetRequiredService<IAiAgentFactory>();
                var mafAgent = agentFactory.CreateAgentAsync(agent).GetAwaiter().GetResult();
                return mafAgent;
            }

            return null!;
        });

        // Add CORS policy for DevUI to allow credentials
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevUICorsPolicy", policy =>
            {
                policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // IMPORTANT: Allow cookies to be sent
            });
        });

        // Configure the Umbraco pipeline to map DevUI endpoints with authentication
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter(
                name: "UmbracoAiDevUI",
                preRouting: applicationBuilder =>
                {
                    // Enable CORS for DevUI endpoints
                    applicationBuilder.UseCors("DevUICorsPolicy");
                },
                postPipeline: applicationBuilder =>
                {
                    // IMPORTANT: Use postPipeline (not postRouting) so this runs AFTER UseAuthentication().
                    // The middleware order in Umbraco is:
                    //   1. PreRouting
                    //   2. UseRouting()
                    //   3. PostRouting
                    //   4. UseAuthentication()  <-- Cookie processing happens here
                    //   5. UseAuthorization()
                    //   6. PostPipeline  <-- Our middleware runs here, after auth
                    // By this point, context.User is set from the authentication cookie.
                    applicationBuilder.UseMiddleware<DevUIAuthorizationMiddleware>();
                },
                endpoints: applicationBuilder =>
                {
                    applicationBuilder.UseEndpoints(endpoints =>
                    {
                        // Map endpoints for OpenAI responses and conversations (required for DevUI)
                        endpoints.MapOpenAIResponses();
                        endpoints.MapOpenAIConversations();

                        // Map all DevUI endpoints
                        // - /umbraco/devui and /meta are protected by DevUIAuthorizationMiddleware
                        // - /v1/entities endpoints have .RequireAuthorization() applied directly
                        endpoints.MapUmbracoAiDevUI();
                    });
                })));

        return builder;
    }
}
