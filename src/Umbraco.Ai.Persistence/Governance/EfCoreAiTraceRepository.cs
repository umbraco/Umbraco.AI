using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Governance;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// EF Core implementation of the AI trace repository.
/// </summary>
internal class EfCoreAiTraceRepository : IAiTraceRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiTraceRepository"/>.
    /// </summary>
    public EfCoreAiTraceRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiTrace?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTraceEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Traces
                .Include(t => t.Spans)
                .FirstOrDefaultAsync(t => t.Id == id, ct));

        scope.Complete();
        return entity is null ? null : AiTraceFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiTrace?> GetByTraceIdAsync(string traceId, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiTraceEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Traces
                .Include(t => t.Spans)
                .FirstOrDefaultAsync(t => t.TraceId == traceId, ct));

        scope.Complete();
        return entity is null ? null : AiTraceFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTrace>, int Total)> GetPagedAsync(
        AiTraceFilter filter, int skip, int take, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiTraceEntity> query = db.Traces;

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
            List<AiTraceEntity> items = await query
                .OrderByDescending(t => t.StartTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiTraceFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTrace>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiTraceEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Traces
                .Where(t => t.EntityId == entityId && t.EntityType == entityType)
                .OrderByDescending(t => t.StartTime)
                .Take(limit)
                .ToListAsync(ct));

        scope.Complete();
        return entities.Select(AiTraceFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AiTrace> SaveAsync(AiTrace trace, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiTraceEntity? existing = await db.Traces.FindAsync([trace.Id], ct);

            if (existing is null)
            {
                // Insert new trace
                AiTraceEntity newEntity = AiTraceFactory.BuildEntity(trace);
                db.Traces.Add(newEntity);
            }
            else
            {
                // Update existing trace
                AiTraceFactory.UpdateEntity(existing, trace);
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
            AiTraceEntity? entity = await db.Traces.FindAsync([id], ct);
            if (entity is null)
            {
                return false;
            }

            db.Traces.Remove(entity);
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
            List<AiTraceEntity> oldTraces = await db.Traces
                .Where(t => t.StartTime < threshold)
                .ToListAsync(ct);

            if (oldTraces.Count == 0)
            {
                return 0;
            }

            db.Traces.RemoveRange(oldTraces);
            await db.SaveChangesAsync(ct);
            return oldTraces.Count;
        });

        scope.Complete();
        return deletedCount;
    }
}
