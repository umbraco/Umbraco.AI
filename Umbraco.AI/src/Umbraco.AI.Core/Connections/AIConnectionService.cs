using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
internal sealed class AIConnectionService : IAIConnectionService
{
    private readonly IAIConnectionRepository _repository;
    private readonly AIProviderCollection _providers;
    private readonly IAIEditableModelResolver _modelResolver;
    private readonly IAIEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AIConnectionService(
        IAIConnectionRepository repository,
        AIProviderCollection providers,
        IAIEditableModelResolver modelResolver,
        IAIEntityVersionService versionService,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _providers = providers;
        _modelResolver = modelResolver;
        _versionService = versionService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AIConnection?> GetConnectionByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        return _repository.GetByAliasAsync(alias, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return _repository.GetAllAsync(cancellationToken);
        }

        return _repository.GetByProviderAsync(providerId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIConnection> Items, int Total)> GetConnectionsPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(filter, providerId, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIConnectionRef>> GetConnectionReferencesAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetByProviderAsync(providerId, cancellationToken);
        return connections.Select(c => new AIConnectionRef(c.Id, c.Name));
    }

    /// <inheritdoc />
    public async Task<AIConnection> SaveConnectionAsync(AIConnection connection, CancellationToken cancellationToken = default)
    {
        // Generate new ID if needed
        if (connection.Id == Guid.Empty)
        {
            connection.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(connection.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != connection.Id)
        {
            throw new InvalidOperationException($"A connection with alias '{connection.Alias}' already exists.");
        }

        // Validate provider exists
        var provider = _providers.GetById(connection.ProviderId);
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{connection.ProviderId}' not found.");
        }

        // Validate settings type if settings provided
        if (connection.Settings is not null)
        {
            await ValidateConnectionAsync(connection.ProviderId, connection.Settings, cancellationToken);
        }

        // Update timestamp
        connection.DateModified = DateTime.UtcNow;

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetAsync(connection.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Save to repository
        return await _repository.SaveAsync(connection, userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"Connection with ID '{id}' not found.");
        }

        // TODO: Check if connection is in use by profiles before deletion
        // This will require IAIProfileService when implemented

        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Connection", cancellationToken);

        await _repository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ValidateConnectionAsync(string providerId, object? settings, CancellationToken cancellationToken = default)
    {
        // Validation is now handled by the settings resolver
        // This will deserialize, resolve env vars, and validate in one step
        try
        {
            var provider = _providers.GetById(providerId);
            if (provider is null)
            {
                throw new InvalidOperationException($"Provider '{providerId}' not found.");
            }

            // ResolveSettingsForProvider will validate the settings
            _modelResolver.ResolveSettingsForProvider(provider, settings);
            return Task.FromResult(true);
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation errors as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to validate settings for provider '{providerId}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var configured = await GetConfiguredProviderAsync(id, cancellationToken);
        if (configured is null)
        {
            throw new InvalidOperationException($"Connection with ID '{id}' not found or provider unavailable.");
        }

        var capability = configured.GetCapabilities().FirstOrDefault();
        if (capability is null)
        {
            throw new InvalidOperationException($"Provider '{configured.Provider.Id}' has no capabilities to test.");
        }

        try
        {
            await capability.GetModelsAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IAIConfiguredProvider?> GetConfiguredProviderAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _repository.GetAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            return null;
        }

        var provider = _providers.GetById(connection.ProviderId);
        if (provider is null)
        {
            return null;
        }

        var resolvedSettings = _modelResolver.ResolveSettingsForProvider(provider, connection.Settings);
        if (resolvedSettings is null)
        {
            return null;
        }

        return new AIConfiguredProvider(provider, resolvedSettings);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AICapability>> GetAvailableCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetAllAsync(cancellationToken);
        var capabilities = new HashSet<AICapability>();

        foreach (var connection in connections)
        {
            var provider = _providers.GetById(connection.ProviderId);
            if (provider is not null)
            {
                foreach (var cap in provider.GetCapabilities())
                {
                    capabilities.Add(cap.Kind);
                }
            }
        }

        return capabilities;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIConnection>> GetConnectionsByCapabilityAsync(AICapability capability, CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetAllAsync(cancellationToken);

        return connections.Where(conn =>
        {
            var provider = _providers.GetById(conn.ProviderId);
            return provider?.GetCapabilities().Any(c => c.Kind == capability) ?? false;
        });
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetConnectionVersionHistoryAsync(
        Guid connectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(connectionId, "Connection", skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<AIConnection?> GetConnectionVersionSnapshotAsync(
        Guid connectionId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AIConnection>(connectionId, version, cancellationToken);

    /// <inheritdoc />
    public async Task<AIConnection> RollbackConnectionAsync(
        Guid connectionId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the current connection to ensure it exists
        var currentConnection = await _repository.GetAsync(connectionId, cancellationToken);
        if (currentConnection is null)
        {
            throw new InvalidOperationException($"Connection with ID '{connectionId}' not found.");
        }

        // Get the snapshot at the target version
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIConnection>(connectionId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for connection '{connectionId}'.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Save the current state to version history before rolling back
        await _versionService.SaveVersionAsync(currentConnection, userId, null, cancellationToken);

        // Create a new version by saving the snapshot data
        // We need to preserve the ID and update the dates appropriately
        var rolledBackConnection = new AIConnection
        {
            Id = connectionId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            ProviderId = snapshot.ProviderId,
            Settings = snapshot.Settings,
            IsActive = snapshot.IsActive,
            // The repository will handle version increment and dates
        };

        return await _repository.SaveAsync(rolledBackConnection, userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectionAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value);
    }
}
