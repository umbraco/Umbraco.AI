using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core implementation of <see cref="IAIConversationShareRepository"/>.
/// </summary>
internal sealed class EFCoreAIConversationShareRepository : IAIConversationShareRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIConversationsDbContext> _scopeProvider;

    public EFCoreAIConversationShareRepository(IEFCoreScopeProvider<UmbracoAIConversationsDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task<(AIConversationShare Share, bool WasCreated)> SaveAsync(
        AIConversationShare share,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.ConversationShares.FirstOrDefaultAsync(
                e => e.ConversationId == share.ConversationId &&
                     e.SharedWithUserKey == share.SharedWithUserKey &&
                     e.DateRevoked == null,
                cancellationToken);

            if (existing is not null)
            {
                existing.ScopeMode = (int)share.ScopeMode;
                existing.CutoffMessageId = share.CutoffMessageId;
                await db.SaveChangesAsync(cancellationToken);
                return (AIConversationShareEntityFactory.BuildDomain(existing), false);
            }

            var entity = AIConversationShareEntityFactory.BuildEntity(share, Guid.NewGuid(), DateTime.UtcNow);
            db.ConversationShares.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return (AIConversationShareEntityFactory.BuildDomain(entity), true);
        });
        scope.Complete();
        return result;
    }

    public async Task<IReadOnlyList<AIConversationShare>> GetActiveByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.ConversationShares.AsNoTracking()
                .Where(e => e.ConversationId == conversationId && e.DateRevoked == null)
                .OrderByDescending(e => e.DateShared)
                .ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(AIConversationShareEntityFactory.BuildDomain).ToList();
    }

    public async Task<AIConversationShare?> GetActiveShareAsync(
        Guid conversationId,
        Guid viewerUserKey,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.ConversationShares.AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.ConversationId == conversationId &&
                         e.SharedWithUserKey == viewerUserKey &&
                         e.DateRevoked == null,
                    cancellationToken));
        scope.Complete();
        return entity is null ? null : AIConversationShareEntityFactory.BuildDomain(entity);
    }

    public async Task<bool> RevokeAsync(
        Guid conversationId,
        Guid userKey,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var revoked = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.ConversationShares.FirstOrDefaultAsync(
                e => e.ConversationId == conversationId &&
                     e.SharedWithUserKey == userKey &&
                     e.DateRevoked == null,
                cancellationToken);

            if (entity is null)
            {
                return false;
            }

            entity.DateRevoked = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });
        scope.Complete();
        return revoked;
    }

    public async Task<(IReadOnlyList<AISharedConversationSummary> Items, int Total)> GetSharedWithUserPagedAsync(
        Guid viewerUserKey,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            // Joined against Conversations so the recipient's "shared with me" list carries the
            // owning conversation's display fields inline — never a follow-up read per row.
            IQueryable<AISharedConversationSummary> query =
                from share in db.ConversationShares.AsNoTracking()
                join conversation in db.Conversations.AsNoTracking()
                    on share.ConversationId equals conversation.Id
                where share.SharedWithUserKey == viewerUserKey && share.DateRevoked == null
                select new AISharedConversationSummary
                {
                    ConversationId = conversation.Id,
                    Title = conversation.Title,
                    OwnerUserKey = conversation.UserKey,
                    ScopeMode = (AIConversationShareScopeMode)share.ScopeMode,
                    DateShared = share.DateShared,
                };

            // Title-only match (never message content) — matching message content would let a
            // search hit/miss leak whether a word exists past a Snapshot share's cutoff, see
            // IAIConversationShareRepository's doc comment on this parameter.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(summary => summary.Title != null && summary.Title.ToLower().Contains(s));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(summary => summary.DateShared)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });
        scope.Complete();
        return result;
    }
}
