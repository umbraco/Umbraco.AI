namespace Umbraco.AI.Core.Settings;

/// <summary>
/// In-memory implementation of the settings repository for development/testing.
/// </summary>
internal sealed class InMemoryAiSettingsRepository : IAISettingsRepository
{
    private AISettings _settings = new();

    /// <inheritdoc />
    public Task<AISettings> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_settings);

    /// <inheritdoc />
    public Task<AISettings> SaveAsync(
        AISettings settings,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }
}
