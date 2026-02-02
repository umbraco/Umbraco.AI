using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Factory for creating configured IChatClient instances.
/// Handles client creation from providers and middleware application.
/// </summary>
public interface IAIChatClientFactory
{
    /// <summary>
    /// Creates a fully configured chat client for the given profile.
    /// </summary>
    /// <param name="profile">The AI profile containing model and connection information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured IChatClient with all middleware applied.</returns>
    Task<IChatClient> CreateClientAsync(AIProfile profile, CancellationToken cancellationToken = default);
}
