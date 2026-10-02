using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Factory for creating configured IAIDecisionClient instances.
/// Handles client creation from providers and middleware application.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionClientFactory
{
    /// <summary>
    /// Creates a fully configured decision client for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured IAIDecisionClient with all middleware applied.</returns>
    Task<IAIDecisionClient> CreateClientAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default);
}
