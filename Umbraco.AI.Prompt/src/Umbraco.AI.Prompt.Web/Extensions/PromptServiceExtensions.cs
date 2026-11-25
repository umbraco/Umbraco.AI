using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Prompt.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIPromptService"/> to support IdOrAlias lookups.
/// </summary>
internal static class PromptServiceExtensions
{
    /// <summary>
    /// Gets a prompt by ID or alias, optionally including prompts the current user may not use.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="includeRestricted">Ask for managed access; only honoured with the Manage prompts right.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found and visible to the caller, otherwise null.</returns>
    public static async Task<Core.Prompts.AIPrompt?> GetPromptAsync(
        this IAIPromptService service,
        IdOrAlias idOrAlias,
        bool includeRestricted,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetPromptAsync(idOrAlias.Id.Value, includeRestricted, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetPromptByAliasAsync(idOrAlias.Alias, includeRestricted, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Gets a prompt by ID or alias to run it for the current user.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt, or null if there is none.</returns>
    /// <exception cref="AIPromptNotAvailableException">Thrown if the prompt is limited to user groups the caller is not in.</exception>
    public static async Task<Core.Prompts.AIPrompt?> GetPromptForUseAsync(
        this IAIPromptService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetPromptForUseAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetPromptForUseByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a prompt ID by ID or alias, optionally including prompts the current user may not use.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The prompt service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="includeRestricted">Ask for managed access; only honoured with the Manage prompts right.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetPromptIdAsync(
        this IAIPromptService service,
        IdOrAlias idOrAlias,
        bool includeRestricted,
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
            var prompt = await service.GetPromptByAliasAsync(idOrAlias.Alias, includeRestricted, cancellationToken);
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
        this IAIPromptService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var promptId = await TryGetPromptIdAsync(service, idOrAlias, includeRestricted: false, cancellationToken);
        if (promptId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a prompt with the id or alias of '{idOrAlias}'");
        }

        return promptId.Value;
    }
}
