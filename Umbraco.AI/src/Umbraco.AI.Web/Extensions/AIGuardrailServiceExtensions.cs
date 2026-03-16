using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIGuardrailService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AIGuardrailServiceExtensions
{
    /// <summary>
    /// Gets a guardrail by ID or alias.
    /// </summary>
    /// <param name="service">The guardrail service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail if found, otherwise null.</returns>
    public static async Task<AIGuardrail?> GetGuardrailAsync(
        this IAIGuardrailService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetGuardrailAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetGuardrailByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a guardrail ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The guardrail service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetGuardrailIdAsync(
        this IAIGuardrailService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        if (idOrAlias.Alias != null)
        {
            var guardrail = await service.GetGuardrailByAliasAsync(idOrAlias.Alias, cancellationToken);
            return guardrail?.Id;
        }

        return null;
    }
}
