using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.AuditLog;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.AuditLog;

/// <summary>
/// EF Core implementation of the AI audit-log repository.
/// </summary>
internal class EfCoreAiAuditLogRepository : IAiAuditLogRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiAuditLogRepository"/>.
    /// </summary>
    public EfCoreAiAuditLogRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AIAuditLog?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIAuditLogEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.AuditLogs
                .FirstOrDefaultAsync(t => t.Id == id, ct));

        scope.Complete();
        return entity is null ? null : AIAuditLogFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AIAuditLog>, int Total)> GetPagedAsync(
        AIAuditLogFilter filter, int skip, int take, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIAuditLogEntity> query = db.AuditLogs;

            // Apply status filter
            if (filter.Status.HasValue)
            {
                int statusValue = (int)filter.Status.Value;
                query = query.Where(t => t.Status == statusValue);
            }

            // Apply user ID filter
            if (!string.IsNullOrEmpty(filter.UserId))
            {
                query = query.Where(t => t.UserId == filter.UserId);
            }

            // Apply profile ID filter
            if (filter.ProfileId.HasValue)
            {
                query = query.Where(t => t.ProfileId == filter.ProfileId.Value);
            }

            // Apply provider ID filter
            if (!string.IsNullOrEmpty(filter.ProviderId))
            {
                query = query.Where(t => t.ProviderId == filter.ProviderId);
            }

            // Apply capability filter
            if (filter.Capability.HasValue)
            {
                int capabilityValue = (int)filter.Capability.Value;
                query = query.Where(t => t.Capability == capabilityValue);
            }

            // Apply feature type filter
            if (!string.IsNullOrEmpty(filter.FeatureType))
            {
                query = query.Where(t => t.FeatureType == filter.FeatureType);
            }

            // Apply feature ID filter
            if (filter.FeatureId.HasValue)
            {
                query = query.Where(t => t.FeatureId == filter.FeatureId.Value);
            }

            // Apply entity ID filter
            if (!string.IsNullOrEmpty(filter.EntityId))
            {
                query = query.Where(t => t.EntityId == filter.EntityId);
            }

            // Apply entity type filter
            if (!string.IsNullOrEmpty(filter.EntityType))
            {
                query = query.Where(t => t.EntityType == filter.EntityType);
            }

            // Apply parent audit-log ID filter
            if (filter.ParentAuditLogId.HasValue)
            {
                query = query.Where(t => t.ParentAuditLogId == filter.ParentAuditLogId.Value);
            }

            // Apply date range filters
            if (filter.FromDate.HasValue)
            {
                query = query.Where(t => t.StartTime >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(t => t.StartTime <= filter.ToDate.Value);
            }

            // Apply search text filter (model ID, error message)
            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                string searchLower = filter.SearchText.ToLower();
                query = query.Where(t =>
                    t.ModelId.ToLower().Contains(searchLower) ||
                    (t.ErrorMessage != null && t.ErrorMessage.ToLower().Contains(searchLower)));
            }

            // Get total count before pagination
            int total = await query.CountAsync(ct);

            // Apply pagination and get items (ordered by most recent first)
            List<AIAuditLogEntity> items = await query
                .OrderByDescending(t => t.StartTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AIAuditLogFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIAuditLog>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIAuditLogEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.AuditLogs
                .Where(t => t.EntityId == entityId && t.EntityType == entityType)
                .OrderByDescending(t => t.StartTime)
                .Take(limit)
                .ToListAsync(ct));

        scope.Complete();
        return entities.Select(AIAuditLogFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AIAuditLog> SaveAsync(AIAuditLog trace, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AIAuditLogEntity? existing = await db.AuditLogs.FindAsync([trace.Id], ct);

            if (existing is null)
            {
                // Insert new audit-log
                AIAuditLogEntity newEntity = AIAuditLogFactory.BuildEntity(trace);
                db.AuditLogs.Add(newEntity);
            }
            else
            {
                // Update existing audit-log
                AIAuditLogFactory.UpdateEntity(existing, trace);
            }

            await db.SaveChangesAsync(ct);
            return null;
        });

        scope.Complete();
        return trace;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AIAuditLogEntity? entity = await db.AuditLogs.FindAsync([id], ct);
            if (entity is null)
            {
                return false;
            }

            db.AuditLogs.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanAsync(DateTime threshold, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        int deletedCount = await scope.ExecuteWithContextAsync(async db =>
        {
            List<AIAuditLogEntity> oldAuditLogs = await db.AuditLogs
                .Where(t => t.StartTime < threshold)
                .ToListAsync(ct);

            if (oldAuditLogs.Count == 0)
            {
                return 0;
            }

            db.AuditLogs.RemoveRange(oldAuditLogs);
            await db.SaveChangesAsync(ct);
            return oldAuditLogs.Count;
        });

        scope.Complete();
        return deletedCount;
    }
}
