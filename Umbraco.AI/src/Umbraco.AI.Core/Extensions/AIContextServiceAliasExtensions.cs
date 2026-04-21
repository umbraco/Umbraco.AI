using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIContextService"/> to support alias-based lookups.
/// </summary>
public static class AIContextServiceAliasExtensions
{
    /// <summary>
    /// Resolves a list of context aliases to their corresponding IDs.
    /// </summary>
    /// <param name="service">The context service.</param>
    /// <param name="aliases">The context aliases to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved context IDs in the same order as the input aliases.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a context alias is not found.</exception>
    public static async Task<IReadOnlyList<Guid>> GetContextIdsByAliasesAsync(
        this IAIContextService service,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken = default)
    {
        var resolvedIds = new List<Guid>(aliases.Count);

        foreach (var alias in aliases)
        {
            var context = await service.GetContextByAliasAsync(alias, cancellationToken);
            if (context is null)
            {
                throw new InvalidOperationException($"AI context with alias '{alias}' not found.");
            }

            resolvedIds.Add(context.Id);
        }

        return resolvedIds;
    }
}
