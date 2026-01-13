using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Audit;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Audit;

/// <summary>
/// EF Core implementation of the AI audit repository.
/// </summary>
internal class EfCoreAiAuditRepository : IAiAuditRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiAuditRepository"/>.
    /// </summary>
    public EfCoreAiAuditRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiAudit?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiAuditEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Audits
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(t => t.Id == id, ct));

        scope.Complete();
        return entity is null ? null : AiAuditFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiAudit?> GetByTraceIdAsync(string traceId, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiAuditEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Audits
                .Include(t => t.Activities)
                .FirstOrDefaultAsync(t => t.TraceId == traceId, ct));

        scope.Complete();
        return entity is null ? null : AiAuditFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiAudit>, int Total)> GetPagedAsync(
        AiAuditFilter filter, int skip, int take, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiAuditEntity> query = db.Audits;

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

            // Apply entity ID filter
            if (!string.IsNullOrEmpty(filter.EntityId))
            {
                query = query.Where(t => t.EntityId == filter.EntityId);
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

            // Apply search text filter (operation type, model ID, error message)
            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                string searchLower = filter.SearchText.ToLower();
                query = query.Where(t =>
                    t.OperationType.ToLower().Contains(searchLower) ||
                    t.ModelId.ToLower().Contains(searchLower) ||
                    (t.ErrorMessage != null && t.ErrorMessage.ToLower().Contains(searchLower)));
            }

            // Get total count before pagination
            int total = await query.CountAsync(ct);

            // Apply pagination and get items (ordered by most recent first)
            List<AiAuditEntity> items = await query
                .OrderByDescending(t => t.StartTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiAuditFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiAudit>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiAuditEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Audits
                .Where(t => t.EntityId == entityId && t.EntityType == entityType)
                .OrderByDescending(t => t.StartTime)
                .Take(limit)
                .ToListAsync(ct));

        scope.Complete();
        return entities.Select(AiAuditFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AiAudit> SaveAsync(AiAudit trace, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiAuditEntity? existing = await db.Audits.FindAsync([trace.Id], ct);

            if (existing is null)
            {
                // Insert new audit
                AiAuditEntity newEntity = AiAuditFactory.BuildEntity(trace);
                db.Audits.Add(newEntity);
            }
            else
            {
                // Update existing audit
                AiAuditFactory.UpdateEntity(existing, trace);
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
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AiAuditEntity? entity = await db.Audits.FindAsync([id], ct);
            if (entity is null)
            {
                return false;
            }

            db.Audits.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanAsync(DateTime threshold, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        int deletedCount = await scope.ExecuteWithContextAsync(async db =>
        {
            List<AiAuditEntity> oldAudits = await db.Audits
                .Where(t => t.StartTime < threshold)
                .ToListAsync(ct);

            if (oldAudits.Count == 0)
            {
                return 0;
            }

            db.Audits.RemoveRange(oldAudits);
            await db.SaveChangesAsync(ct);
            return oldAudits.Count;
        });

        scope.Complete();
        return deletedCount;
    }
}
