using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Core.Middleware;

/// <summary>
/// Extension methods for registering AI middleware in an Umbraco-style builder pattern.
/// </summary>
public static class AiMiddlewareExtensions
{
    /// <summary>
    /// Adds a chat middleware to the AI pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type to add.</typeparam>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddAiChatMiddleware<TMiddleware>(this IUmbracoBuilder builder)
        where TMiddleware : class, IAiChatMiddleware
    {
        builder.Services.AddSingleton<IAiChatMiddleware, TMiddleware>();
        return builder;
    }

    /// <summary>
    /// Adds a chat middleware to the AI pipeline with custom factory.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <param name="factory">Factory function to create the middleware.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddAiChatMiddleware(
        this IUmbracoBuilder builder,
        Func<IServiceProvider, IAiChatMiddleware> factory)
    {
        builder.Services.AddSingleton<IAiChatMiddleware>(factory);
        return builder;
    }

    /// <summary>
    /// Adds an embedding middleware to the AI pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type to add.</typeparam>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddAiEmbeddingMiddleware<TMiddleware>(this IUmbracoBuilder builder)
        where TMiddleware : class, IAiEmbeddingMiddleware
    {
        builder.Services.AddSingleton<IAiEmbeddingMiddleware, TMiddleware>();
        return builder;
    }

    /// <summary>
    /// Adds an embedding middleware to the AI pipeline with custom factory.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <param name="factory">Factory function to create the middleware.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddAiEmbeddingMiddleware(
        this IUmbracoBuilder builder,
        Func<IServiceProvider, IAiEmbeddingMiddleware> factory)
    {
        builder.Services.AddSingleton<IAiEmbeddingMiddleware>(factory);
        return builder;
    }
}
