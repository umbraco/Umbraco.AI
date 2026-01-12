using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Governance;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// EF Core implementation of the AI execution span repository.
/// </summary>
internal class EfCoreAiExecutionSpanRepository : IAiExecutionSpanRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiExecutionSpanRepository"/>.
    /// </summary>
    public EfCoreAiExecutionSpanRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<IEnumerable<AiExecutionSpan>> GetByTraceIdAsync(Guid traceId, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiExecutionSpanEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.ExecutionSpans
                .Where(s => s.TraceId == traceId)
                .OrderBy(s => s.SequenceNumber)
                .ToListAsync(ct));

        scope.Complete();
        return entities.Select(AiExecutionSpanFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<AiExecutionSpan> SaveAsync(AiExecutionSpan span, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiExecutionSpanEntity? existing = await db.ExecutionSpans.FindAsync([span.Id], ct);

            if (existing is null)
            {
                // Insert new span
                AiExecutionSpanEntity newEntity = AiExecutionSpanFactory.BuildEntity(span);
                db.ExecutionSpans.Add(newEntity);
            }
            else
            {
                // Update existing span
                AiExecutionSpanFactory.UpdateEntity(existing, span);
            }

            await db.SaveChangesAsync(ct);
            return null;
        });

        scope.Complete();
        return span;
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(IEnumerable<AiExecutionSpan> spans, CancellationToken ct)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            foreach (AiExecutionSpan span in spans)
            {
                AiExecutionSpanEntity? existing = await db.ExecutionSpans.FindAsync([span.Id], ct);

                if (existing is null)
                {
                    AiExecutionSpanEntity newEntity = AiExecutionSpanFactory.BuildEntity(span);
                    db.ExecutionSpans.Add(newEntity);
                }
                else
                {
                    AiExecutionSpanFactory.UpdateEntity(existing, span);
                }
            }

            await db.SaveChangesAsync(ct);
            return null;
        });

        scope.Complete();
    }
}
