using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Service interface for prompt management operations.
/// </summary>
public interface IAIPromptService
{
    /// <summary>
    /// Gets a prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found and the current user may use it, null otherwise.</returns>
    /// <remarks>
    /// A prompt limited to user groups the current user is not in is treated as not found.
    /// Use <see cref="GetPromptAsync(Guid, bool, CancellationToken)"/> to manage prompts the user can't use.
    /// </remarks>
    Task<AIPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its unique identifier, optionally including prompts the current user may not use.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="includeRestricted">
    /// Ask for managed access: return the prompt even when it is limited to other user groups. Only honoured
    /// when the current user has the Manage prompts right; otherwise it is ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found and visible to the caller, null otherwise.</returns>
    /// <remarks>
    /// The default implementation exists so that third-party implementations of this interface keep compiling;
    /// it ignores <paramref name="includeRestricted"/> and returns what <see cref="GetPromptAsync(Guid, CancellationToken)"/> does.
    /// </remarks>
    Task<AIPrompt?> GetPromptAsync(Guid id, bool includeRestricted, CancellationToken cancellationToken = default)
        => GetPromptAsync(id, cancellationToken);

    /// <summary>
    /// Gets a prompt by its alias.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found and the current user may use it, null otherwise.</returns>
    /// <remarks>
    /// A prompt limited to user groups the current user is not in is treated as not found.
    /// </remarks>
    Task<AIPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its alias, optionally including prompts the current user may not use.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="includeRestricted">
    /// Ask for managed access: return the prompt even when it is limited to other user groups. Only honoured
    /// when the current user has the Manage prompts right; otherwise it is ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found and visible to the caller, null otherwise.</returns>
    /// <remarks>
    /// The default implementation exists so that third-party implementations of this interface keep compiling;
    /// it ignores <paramref name="includeRestricted"/> and returns what <see cref="GetPromptByAliasAsync(string, CancellationToken)"/> does.
    /// </remarks>
    Task<AIPrompt?> GetPromptByAliasAsync(string alias, bool includeRestricted, CancellationToken cancellationToken = default)
        => GetPromptByAliasAsync(alias, cancellationToken);

    /// <summary>
    /// Gets a prompt by ID to run it for the current user.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt, or null if there is none with that ID.</returns>
    /// <exception cref="AIPromptNotAvailableException">Thrown if the prompt is limited to user groups the caller is not in.</exception>
    /// <remarks>
    /// Unlike <see cref="GetPromptAsync(Guid, CancellationToken)"/>, a limited prompt is refused, not read as
    /// missing, so the caller can tell "not available to you" from "doesn't exist". The default implementation
    /// exists so that third-party implementations of this interface keep compiling; it reads a limited prompt as missing.
    /// </remarks>
    Task<AIPrompt?> GetPromptForUseAsync(Guid id, CancellationToken cancellationToken = default)
        => GetPromptAsync(id, cancellationToken);

    /// <summary>
    /// Gets a prompt by alias to run it for the current user.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt, or null if there is none with that alias.</returns>
    /// <exception cref="AIPromptNotAvailableException">Thrown if the prompt is limited to user groups the caller is not in.</exception>
    /// <remarks>
    /// See <see cref="GetPromptForUseAsync(Guid, CancellationToken)"/> for how this differs from
    /// <see cref="GetPromptByAliasAsync(string, CancellationToken)"/>.
    /// </remarks>
    Task<AIPrompt?> GetPromptForUseByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => GetPromptByAliasAsync(alias, cancellationToken);

    /// <summary>
    /// Gets all prompts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompts the current user may use.</returns>
    Task<IEnumerable<AIPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of prompts with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated prompts and the total count, limited to the prompts the current user may use.</returns>
    Task<(IEnumerable<AIPrompt> Items, int Total)> GetPromptsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of prompts with optional filtering, optionally including prompts the current user may not use.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="includeRestricted">
    /// Ask for managed access: return every prompt, limited or not. Only honoured when the current user has
    /// the Manage prompts right; otherwise it is ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated prompts and the total count.</returns>
    /// <remarks>
    /// The default implementation exists so that third-party implementations of this interface keep compiling;
    /// it ignores <paramref name="includeRestricted"/> and returns what the overload without it does.
    /// </remarks>
    Task<(IEnumerable<AIPrompt> Items, int Total)> GetPromptsPagedAsync(
        int skip,
        int take,
        string? filter,
        Guid? profileId,
        bool includeRestricted,
        CancellationToken cancellationToken = default)
        => GetPromptsPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <summary>
    /// Save a prompt (insert if new, update if exists) with validation.
    /// If prompt.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="prompt">The prompt to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved prompt.</returns>
    Task<AIPrompt> SavePromptAsync(AIPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a prompt.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any prompts reference the specified profile.
    /// </summary>
    /// <param name="profileId">The profile ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if one or more prompts reference the profile, otherwise false.</returns>
    Task<bool> PromptsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given alias exists.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt and returns the AI response.
    /// </summary>
    /// <param name="promptId">The prompt ID to execute.</param>
    /// <param name="request">The execution request containing context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result containing the AI response.</returns>
    /// <exception cref="AIPromptNotAvailableException">Thrown if the prompt is limited to user groups the caller is not in.</exception>
    Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with execution options controlling validation and overrides.
    /// </summary>
    /// <param name="promptId">The prompt ID to execute.</param>
    /// <param name="request">The execution request containing context.</param>
    /// <param name="options">Options controlling scope validation, profile, and context overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result containing the AI response.</returns>
    /// <exception cref="AIPromptNotAvailableException">Thrown if the prompt is limited to user groups the caller is not in.</exception>
    Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        AIPromptExecutionOptions options,
        CancellationToken cancellationToken = default);
}
