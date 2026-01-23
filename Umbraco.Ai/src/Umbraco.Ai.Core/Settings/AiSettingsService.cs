using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Service for managing AI settings.
/// </summary>
internal sealed class AiSettingsService : IAiSettingsService
{
    private readonly IAiSettingsRepository _repository;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AiSettingsService(
        IAiSettingsRepository repository,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public async Task<AiSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        => await _repository.GetAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiSettings> SaveSettingsAsync(
        AiSettings settings,
        CancellationToken cancellationToken = default)
    {
        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Id;
        return await _repository.SaveAsync(settings, userId, cancellationToken);
    }
}
