using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Persistence.Entities;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the AI connection repository.
/// </summary>
internal class EfCoreAiConnectionRepository : IAiConnectionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiConnectionRepository"/>.
    /// </summary>
    public EfCoreAiConnectionRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiConnectionEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiConnectionEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections
                .Where(c => c.ProviderId == providerId)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<AiConnection> SaveAsync(AiConnection connection, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiConnectionEntity? existing = await db.Connections.FindAsync([connection.Id], cancellationToken);

            if (existing is null)
            {
                AiConnectionEntity newEntity = MapToEntity(connection);
                db.Connections.Add(newEntity);
            }
            else
            {
                UpdateEntity(existing, connection);
            }

            await db.SaveChangesAsync(cancellationToken);
            return null;
        });

        scope.Complete();
        return connection;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AiConnectionEntity? entity = await db.Connections.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Connections.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool exists = await scope.ExecuteWithContextAsync(async db =>
            await db.Connections.AnyAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return exists;
    }

    private static AiConnection MapToDomain(AiConnectionEntity entity)
    {
        object? settings = null;
        if (!string.IsNullOrEmpty(entity.SettingsJson))
        {
            // Settings are stored as JSON, deserialize to dynamic object
            // The actual typed deserialization happens at the service layer
            settings = JsonSerializer.Deserialize<JsonElement>(entity.SettingsJson);
        }

        return new AiConnection
        {
            Id = entity.Id,
            Name = entity.Name,
            ProviderId = entity.ProviderId,
            Settings = settings,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified
        };
    }

    private static AiConnectionEntity MapToEntity(AiConnection connection)
    {
        return new AiConnectionEntity
        {
            Id = connection.Id,
            Name = connection.Name,
            ProviderId = connection.ProviderId,
            SettingsJson = connection.Settings is null ? null : JsonSerializer.Serialize(connection.Settings),
            IsActive = connection.IsActive,
            DateCreated = connection.DateCreated,
            DateModified = connection.DateModified
        };
    }

    private static void UpdateEntity(AiConnectionEntity entity, AiConnection connection)
    {
        entity.Name = connection.Name;
        entity.ProviderId = connection.ProviderId;
        entity.SettingsJson = connection.Settings is null ? null : JsonSerializer.Serialize(connection.Settings);
        entity.IsActive = connection.IsActive;
        entity.DateModified = connection.DateModified;
    }
}
