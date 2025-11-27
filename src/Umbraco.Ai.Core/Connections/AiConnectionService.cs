using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
internal sealed class AiConnectionService : IAiConnectionService
{
    private readonly IAiConnectionRepository _repository;
    private readonly IAiRegistry _registry;
    private readonly IAiSettingsResolver _settingsResolver;

    public AiConnectionService(
        IAiConnectionRepository repository,
        IAiRegistry registry,
        IAiSettingsResolver settingsResolver)
    {
        _repository = repository;
        _registry = registry;
        _settingsResolver = settingsResolver;
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
            connection = new AiConnection
            {
                Id = Guid.NewGuid(),
                Alias = connection.Alias,
                Name = connection.Name,
                ProviderId = connection.ProviderId,
                Settings = connection.Settings,
                IsActive = connection.IsActive,
                DateCreated = connection.DateCreated,
                DateModified = connection.DateModified
            };
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(connection.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != connection.Id)
        {
            throw new InvalidOperationException($"A connection with alias '{connection.Alias}' already exists.");
        }

        // Validate provider exists
        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{connection.ProviderId}' not found in registry.");
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
            var provider = _registry.GetProvider(providerId);
            if (provider is null)
            {
                throw new InvalidOperationException($"Provider '{providerId}' not found in registry.");
            }

            // ResolveSettingsForProvider will validate the settings
            _settingsResolver.ResolveSettingsForProvider(provider, settings);
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
        // Get connection
        var connection = await _repository.GetAsync(id, cancellationToken);
        if (connection is null)
        {
            throw new InvalidOperationException($"Connection with ID '{id}' not found.");
        }

        // Get provider
        var provider = _registry.GetProvider(connection.ProviderId);
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{connection.ProviderId}' not found in registry.");
        }

        // Try to get any capability to test
        var capabilities = provider.GetCapabilities();
        if (!capabilities.Any())
        {
            throw new InvalidOperationException($"Provider '{connection.ProviderId}' has no capabilities to test.");
        }

        // Try to call GetModelsAsync to verify authentication
        try
        {
            var capability = capabilities.First();
            await capability.GetModelsAsync(connection.Settings, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
