using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
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

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AISettingsSavingNotification(settings, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Settings save cancelled: {errorMessages}");
        }

        // Perform save
        var settingResult = await _repository.SaveAsync(settings, userId, cancellationToken);
        _cache.Insert(SettingsCacheKey, () => settingResult);

        // Publish saved notification (after save)
        var savedNotification = new AISettingsSavedNotification(settingResult, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return settingResult;
    }
}
