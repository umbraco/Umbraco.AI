using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Audit;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Audit;

/// <summary>
/// EF Core implementation of the AI execution span repository.
/// </summary>
internal class EfCoreAiAuditActivityRepository : IAiAuditActivityRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiAuditActivityRepository"/>.
    /// </summary>
    public EfCoreAiAuditActivityRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<IEnumerable<AiAuditActivity>> GetByAuditIdAsync(Guid auditId, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiAuditActivityEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.AuditActivities
                .Where(s => s.AuditId == auditId)
                .OrderBy(s => s.SequenceNumber)
                .ToListAsync(ct));

        scope.Complete();
        return entities.Select(AiAuditActivityFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AiAuditActivity> SaveAsync(AiAuditActivity span, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiAuditActivityEntity? existing = await db.AuditActivities.FindAsync([span.Id], ct);

            if (existing is null)
            {
                // Insert new span
                AiAuditActivityEntity newEntity = AiAuditActivityFactory.BuildEntity(span);
                db.AuditActivities.Add(newEntity);
            }
            else
            {
                // Update existing span
                AiAuditActivityFactory.UpdateEntity(existing, span);
            }

            await db.SaveChangesAsync(ct);
            return null;
        });

        scope.Complete();
        return span;
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(IEnumerable<AiAuditActivity> Activities, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            foreach (AiAuditActivity span in Activities)
            {
                AiAuditActivityEntity? existing = await db.AuditActivities.FindAsync([span.Id], ct);

                if (existing is null)
                {
                    AiAuditActivityEntity newEntity = AiAuditActivityFactory.BuildEntity(span);
                    db.AuditActivities.Add(newEntity);
                }
                else
                {
                    AiAuditActivityFactory.UpdateEntity(existing, span);
                }
            }

            await db.SaveChangesAsync(ct);
            return null;
        });

        scope.Complete();
    }
}
