using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIOrchestrationService"/> to support IdOrAlias lookups.
/// </summary>
internal static class OrchestrationServiceExtensions
{
    /// <summary>
    /// Gets an orchestration by ID or alias.
    /// </summary>
    /// <param name="service">The orchestration service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found, otherwise null.</returns>
    public static async Task<AIOrchestration?> GetOrchestrationAsync(
        this IAIOrchestrationService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetOrchestrationAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetOrchestrationByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get an orchestration ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The orchestration service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetOrchestrationIdAsync(
        this IAIOrchestrationService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        if (idOrAlias.Alias != null)
        {
            var orchestration = await service.GetOrchestrationByAliasAsync(idOrAlias.Alias, cancellationToken);
            return orchestration?.Id;
        }

        return null;
    }
}
