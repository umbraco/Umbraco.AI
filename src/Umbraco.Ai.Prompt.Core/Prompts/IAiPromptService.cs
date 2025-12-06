using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service interface for prompt management operations.
/// </summary>
public interface IAiPromptService
{
    /// <summary>
    /// Gets a prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AiPrompt?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its alias.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AiPrompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all prompts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All prompts.</returns>
    Task<IEnumerable<AiPrompt>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of prompts with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing prompts and total count.</returns>
    Task<PagedModel<AiPrompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a prompt (insert if new, update if exists) with validation.
    /// If prompt.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="prompt">The prompt to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved prompt.</returns>
    Task<AiPrompt> SavePromptAsync(AiPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a prompt.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given alias exists.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt and returns the AI response.
    /// </summary>
    /// <param name="promptId">The prompt ID to execute.</param>
    /// <param name="request">The execution request containing context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result containing the AI response.</returns>
    Task<AiPromptExecutionResult> ExecuteAsync(
        Guid promptId,
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken = default);
}
