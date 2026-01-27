using Umbraco.Ai.DevUI.Models;

namespace Umbraco.Ai.DevUI.Services;

/// <summary>
/// Service for discovering and providing entity information for DevUI.
/// </summary>
public interface IDevUIEntityDiscoveryService
{
    /// <summary>
    /// Gets all discoverable entities (framework agents and Umbraco agents).
    /// </summary>
    Task<DevUIDiscoveryResponse> GetAllEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information for a specific entity by ID.
    /// </summary>
    Task<DevUIEntityInfo?> GetEntityInfoAsync(string entityId, CancellationToken cancellationToken = default);
}
