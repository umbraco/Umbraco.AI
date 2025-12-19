using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiAgentService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AgentserviceExtensions
{
    /// <summary>
    /// Gets a prompt by ID or alias.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, otherwise null.</returns>
    public static async Task<Core.Agents.AiAgent?> GetPromptAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a prompt ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetPromptIdAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        // If it's already an ID, return it directly (no DB lookup needed)
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        // For alias, we need to look up the prompt
        if (idOrAlias.Alias != null)
        {
            var prompt = await service.GetByAliasAsync(idOrAlias.Alias, cancellationToken);
            return prompt?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a prompt ID by ID or alias, throwing if not found.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the prompt is not found.</exception>
    public static async Task<Guid> GetPromptIdAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var promptId = await TryGetPromptIdAsync(service, idOrAlias, cancellationToken);
        if (promptId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a prompt with the id or alias of '{idOrAlias}'");
        }

        return promptId.Value;
    }
}
