using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Service for managing AI settings.
/// </summary>
internal sealed class AISettingsService : IAISettingsService
{
    private readonly IAISettingsRepository _repository;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly IAppPolicyCache _cache;
    private readonly IEventAggregator _eventAggregator;

    private const string SettingsCacheKey = "Umbraco.AI.Settings";

    public AISettingsService(
        IAISettingsRepository repository,
        IAppPolicyCache cache,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _cache = cache;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public async Task<AISettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        => (await _cache.GetCacheItemAsync(
            SettingsCacheKey,
            async () => await _repository.GetAsync(cancellationToken),
            null))!;

    /// <inheritdoc />
    public async Task<AISettings> SaveSettingsAsync(
        AISettings settings,
        CancellationToken cancellationToken = default)
    {
        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;
        var settingResult = await _repository.SaveAsync(settings, userId, cancellationToken);
        _cache.Insert(SettingsCacheKey,() => settingResult);

        // Publish saved notification for Deploy integration
        var savedNotification = new AISettingsSavedNotification(settingResult, new[] { settingResult });
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return settingResult;
    }
}
