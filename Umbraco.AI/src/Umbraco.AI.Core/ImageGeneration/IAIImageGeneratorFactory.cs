using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Factory for creating configured <see cref="IImageGenerator"/> instances.
/// Handles generator creation from providers and middleware application.
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public interface IAIImageGeneratorFactory
{
    /// <summary>
    /// Creates a fully configured image generator for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured <see cref="IImageGenerator"/> with all middleware applied.</returns>
    Task<IImageGenerator> CreateGeneratorAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default);
}
