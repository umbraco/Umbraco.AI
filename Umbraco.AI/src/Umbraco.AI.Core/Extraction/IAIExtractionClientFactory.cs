using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Factory for creating configured IAIExtractionClient instances.
/// Handles client creation from providers and middleware application.
/// </summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public interface IAIExtractionClientFactory
{
    /// <summary>
    /// Creates a fully configured extraction client for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured IAIExtractionClient with all middleware applied.</returns>
    Task<IAIExtractionClient> CreateClientAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default);
}
