namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Repository interface for AI settings persistence.
/// </summary>
internal interface IAISettingsRepository
{
    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    Task<AISettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the AI settings.
    /// </summary>
    Task<AISettings> SaveAsync(AISettings settings, Guid? userId = null, CancellationToken cancellationToken = default);
}
