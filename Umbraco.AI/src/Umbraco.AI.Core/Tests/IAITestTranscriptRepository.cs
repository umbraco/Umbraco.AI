namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines a repository for managing test transcripts.
/// Internal implementation detail - use <see cref="IAITestService"/> for external access.
/// </summary>
internal interface IAITestTranscriptRepository
{
    /// <summary>
    /// Gets a transcript by its unique identifier.
    /// </summary>
    Task<AITestTranscript?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transcript by run ID.
    /// </summary>
    Task<AITestTranscript?> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a transcript.
    /// </summary>
    Task<AITestTranscript> SaveAsync(AITestTranscript transcript, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transcript by its unique identifier.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
