using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiContextService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AiContextServiceExtensions
{
    /// <summary>
    /// Gets a context by ID or alias.
    /// </summary>
    /// <param name="service">The context service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context if found, otherwise null.</returns>
    public static async Task<AiContext?> GetContextAsync(
        this IAiContextService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetContextAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetContextByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a context ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The context service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetContextIdAsync(
        this IAiContextService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        // If it's already an ID, return it directly (no DB lookup needed)
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        // For alias, we need to look up the context
        if (idOrAlias.Alias != null)
        {
            var context = await service.GetContextByAliasAsync(idOrAlias.Alias, cancellationToken);
            return context?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a context ID by ID or alias, throwing if not found.
    /// </summary>
    /// <param name="service">The context service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context is not found.</exception>
    public static async Task<Guid> GetContextIdAsync(
        this IAiContextService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var contextId = await TryGetContextIdAsync(service, idOrAlias, cancellationToken);
        if (contextId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a context with the id or alias of '{idOrAlias}'");
        }

        return contextId.Value;
    }
}
