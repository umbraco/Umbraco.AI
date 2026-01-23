namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// In-memory implementation of the settings repository for development/testing.
/// </summary>
internal sealed class InMemoryAiSettingsRepository : IAiSettingsRepository
{
    private AiSettings _settings = new();

    /// <inheritdoc />
    public Task<AiSettings> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_settings);

    /// <inheritdoc />
    public Task<AiSettings> SaveAsync(
        AiSettings settings,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }
}
