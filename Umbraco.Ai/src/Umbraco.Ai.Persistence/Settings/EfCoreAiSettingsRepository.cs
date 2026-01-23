using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Settings;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Settings;

/// <summary>
/// EF Core implementation of the settings repository.
/// </summary>
internal sealed class EfCoreAiSettingsRepository : IAiSettingsRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    public EfCoreAiSettingsRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AiSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Settings.ToListAsync(cancellationToken));

        scope.Complete();
        return AiSettingsFactory.BuildDomain(entities);
    }

    /// <inheritdoc />
    public async Task<AiSettings> SaveAsync(
        AiSettings settings,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var updatedEntities = await scope.ExecuteWithContextAsync(async db =>
        {
            // Get existing entities
            var existingEntities = await db.Settings.ToListAsync(cancellationToken);
            var existingDict = existingEntities.ToDictionary(e => e.Key, e => e);

            // Build updated entities
            var updatedEntities = AiSettingsFactory.BuildEntities(settings, existingEntities, userId).ToList();

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
        return AiSettingsFactory.BuildDomain(updatedEntities);
    }
}
