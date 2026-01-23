namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Repository interface for AI settings persistence.
/// </summary>
internal interface IAiSettingsRepository
{
    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    Task<AiSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the AI settings.
    /// </summary>
    Task<AiSettings> SaveAsync(AiSettings settings, int? userId = null, CancellationToken cancellationToken = default);
}
