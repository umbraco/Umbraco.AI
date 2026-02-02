using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Settings;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Settings;

/// <summary>
/// EF Core implementation of the settings repository.
/// </summary>
internal sealed class EfCoreAiSettingsRepository : IAiSettingsRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAiSettingsRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AISettings> GetAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Settings.ToListAsync(cancellationToken));

        scope.Complete();
        return AISettingsFactory.BuildDomain(entities);
    }

    /// <inheritdoc />
    public async Task<AISettings> SaveAsync(
        AISettings settings,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var updatedEntities = await scope.ExecuteWithContextAsync(async db =>
        {
            // Get existing entities
            var existingEntities = await db.Settings.ToListAsync(cancellationToken);
            var existingDict = existingEntities.ToDictionary(e => e.Key, e => e);

            // Build updated entities
            var updatedEntities = AISettingsFactory.BuildEntities(settings, existingEntities, userId).ToList();

            foreach (var entity in updatedEntities)
            {
                if (existingDict.ContainsKey(entity.Key))
                {
                    // Entity was modified in-place by BuildEntities
                    db.Entry(entity).State = EntityState.Modified;
                }
                else
                {
                    // New entity
                    await db.Settings.AddAsync(entity, cancellationToken);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return updatedEntities;
        });

        scope.Complete();
        return AISettingsFactory.BuildDomain(updatedEntities);
    }
}
