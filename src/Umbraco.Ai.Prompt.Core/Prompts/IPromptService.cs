using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service interface for prompt management operations.
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Gets a prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<Prompt?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its alias.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<Prompt?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all prompts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All prompts.</returns>
    Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets prompts associated with a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prompts linked to the profile.</returns>
    Task<IEnumerable<Prompt>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of prompts with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing prompts and total count.</returns>
    Task<PagedModel<Prompt>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new prompt.
    /// </summary>
    /// <param name="alias">Unique alias.</param>
    /// <param name="name">Display name.</param>
    /// <param name="content">Prompt content.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="profileId">Optional linked profile ID.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created prompt.</returns>
    Task<Prompt> CreateAsync(
        string alias,
        string name,
        string content,
        string? description = null,
        Guid? profileId = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing prompt.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="name">Display name.</param>
    /// <param name="content">Prompt content.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="profileId">Optional linked profile ID.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="isActive">Whether the prompt is active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated prompt, or null if not found.</returns>
    Task<Prompt?> UpdateAsync(
        Guid id,
        string name,
        string content,
        string? description = null,
        Guid? profileId = null,
        IEnumerable<string>? tags = null,
        bool isActive = true,
        CancellationToken cancellationToken = default);

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
}
