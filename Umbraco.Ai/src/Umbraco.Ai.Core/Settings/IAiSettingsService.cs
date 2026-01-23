namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Service for managing AI settings.
/// </summary>
public interface IAiSettingsService
{
    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    Task<AiSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the AI settings.
    /// </summary>
    Task<AiSettings> SaveSettingsAsync(AiSettings settings, CancellationToken cancellationToken = default);
}
