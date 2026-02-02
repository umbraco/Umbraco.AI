namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Service for managing AI settings.
/// </summary>
public interface IAISettingsService
{
    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    Task<AISettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the AI settings.
    /// </summary>
    Task<AISettings> SaveSettingsAsync(AISettings settings, CancellationToken cancellationToken = default);
}
