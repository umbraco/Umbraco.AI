using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Repository interface for prompt persistence operations.
/// Internal implementation detail - use <see cref="IAiPromptService"/> for external access.
/// </summary>
internal interface IAIPromptRepository
{
    /// <summary>
    /// Gets a prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AIPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its alias.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AIPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all prompts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All prompts.</returns>
    Task<IEnumerable<AIPrompt>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of prompts with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing prompts and total count.</returns>
    Task<PagedModel<AIPrompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a prompt (creates or updates).
    /// </summary>
    /// <param name="prompt">The prompt to save.</param>
    /// <param name="userId">Optional user key (GUID) for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved prompt.</returns>
    Task<AIPrompt> SaveAsync(AIPrompt prompt, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a prompt by its ID.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given ID exists.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given alias exists.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
