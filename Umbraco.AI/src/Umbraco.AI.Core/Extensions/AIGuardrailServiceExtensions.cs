using Umbraco.AI.Core.Guardrails;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIGuardrailService"/> to support alias-based lookups.
/// </summary>
public static class AIGuardrailServiceAliasExtensions
{
    /// <summary>
    /// Resolves a list of guardrail aliases to their corresponding IDs.
    /// </summary>
    /// <param name="service">The guardrail service.</param>
    /// <param name="aliases">The guardrail aliases to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved guardrail IDs in the same order as the input aliases.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a guardrail alias is not found.</exception>
    public static async Task<IReadOnlyList<Guid>> GetGuardrailIdsByAliasesAsync(
        this IAIGuardrailService service,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken = default)
    {
        var resolvedIds = new List<Guid>(aliases.Count);

        foreach (var alias in aliases)
        {
            var guardrail = await service.GetGuardrailByAliasAsync(alias, cancellationToken);
            if (guardrail is null)
            {
                throw new InvalidOperationException($"AI guardrail with alias '{alias}' not found.");
            }

            resolvedIds.Add(guardrail.Id);
        }

        return resolvedIds;
    }
}
