using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
internal sealed class AiConnectionService : IAiConnectionService
{
    private readonly IAiConnectionRepository _repository;
    private readonly AiProviderCollection _providers;
    private readonly IAiEditableModelResolver _modelResolver;

    public AiConnectionService(
        IAiConnectionRepository repository,
        AiProviderCollection providers,
        IAiEditableModelResolver modelResolver)
    {
        _repository = repository;
        _providers = providers;
        _modelResolver = modelResolver;
    }

    /// <inheritdoc />
    public Task<AiConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AiConnection?> GetConnectionByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        return _repository.GetByAliasAsync(alias, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return _repository.GetAllAsync(cancellationToken);
        }

        return _repository.GetByProviderAsync(providerId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AiConnection> Items, int Total)> GetConnectionsPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(filter, providerId, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiConnectionRef>> GetConnectionReferencesAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetByProviderAsync(providerId, cancellationToken);
        return connections.Select(c => new AiConnectionRef(c.Id, c.Name));
    }

    /// <inheritdoc />
    public async Task<AiConnection> SaveConnectionAsync(AiConnection connection, CancellationToken cancellationToken = default)
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

        // Save to repository
        return await _repository.SaveAsync(connection, cancellationToken);
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
        // This will require IAiProfileService when implemented

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
    public async Task<IAiConfiguredProvider?> GetConfiguredProviderAsync(Guid connectionId, CancellationToken cancellationToken = default)
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

        return new AiConfiguredProvider(provider, resolvedSettings);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiCapability>> GetAvailableCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetAllAsync(cancellationToken);
        var capabilities = new HashSet<AiCapability>();

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
    public async Task<IEnumerable<AiConnection>> GetConnectionsByCapabilityAsync(AiCapability capability, CancellationToken cancellationToken = default)
    {
        var connections = await _repository.GetAllAsync(cancellationToken);

        return connections.Where(conn =>
        {
            var provider = _providers.GetById(conn.ProviderId);
            return provider?.GetCapabilities().Any(c => c.Kind == capability) ?? false;
        });
    }
}
