using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiConnectionService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AiConnectionServiceExtensions
{
    /// <summary>
    /// Gets a connection by ID or alias.
    /// </summary>
    /// <param name="service">The connection service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection if found, otherwise null.</returns>
    public static async Task<AiConnection?> GetConnectionAsync(
        this IAiConnectionService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetConnectionAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetConnectionByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a connection ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The connection service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetConnectionIdAsync(
        this IAiConnectionService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        // If it's already an ID, return it directly (no DB lookup needed)
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        // For alias, we need to look up the connection
        if (idOrAlias.Alias != null)
        {
            var connection = await service.GetConnectionByAliasAsync(idOrAlias.Alias, cancellationToken);
            return connection?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a connection ID by ID or alias, throwing if not found.
    /// </summary>
    /// <param name="service">The connection service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not found.</exception>
    public static async Task<Guid> GetConnectionIdAsync(
        this IAiConnectionService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var connectionId = await TryGetConnectionIdAsync(service, idOrAlias, cancellationToken);
        if (connectionId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a connection with the id or alias of '{idOrAlias}'");
        }

        return connectionId.Value;
    }
}
