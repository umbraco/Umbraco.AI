using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Factory for creating configured ISpeechToTextClient instances.
/// Handles client creation from providers and middleware application.
/// </summary>
public interface IAISpeechToTextClientFactory
{
    /// <summary>
    /// Creates a fully configured speech-to-text client for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured ISpeechToTextClient with all middleware applied.</returns>
    Task<ISpeechToTextClient> CreateClientAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default);
}
