using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core implementation of <see cref="IAIConversationRepository"/>.
/// </summary>
internal sealed class EFCoreAIConversationRepository : IAIConversationRepository
{
    private const int MaxAppendRetries = 3;

    private readonly IEFCoreScopeProvider<UmbracoAIConversationsDbContext> _scopeProvider;
    private readonly AIConversationEntityFactory _factory;

    public EFCoreAIConversationRepository(
        IEFCoreScopeProvider<UmbracoAIConversationsDbContext> scopeProvider,
        AIConversationEntityFactory factory)
    {
        _scopeProvider = scopeProvider;
        _factory = factory;
    }

    // --- Conversations ---

    public async Task<AIConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Conversations.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is null)
            {
                return ((AIConversationEntity?)null, new List<AIConversationResourceEntity>());
            }

            var resources = await db.ConversationResources.AsNoTracking()
                .Where(r => r.ConversationId == id)
                .ToListAsync(cancellationToken);

            return (entity, resources);
        });
        scope.Complete();
        return result.Item1 is null ? null : _factory.BuildDomain(result.Item1, result.Item2);
    }

    public async Task<(IReadOnlyList<AIConversation> Items, int Total)> GetPagedAsync(
        Guid userKey,
        int skip,
        int take,
        Guid? projectId = null,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIConversationEntity> query = db.Conversations.AsNoTracking()
                .Where(c => c.UserKey == userKey);

            if (!includeArchived)
            {
                query = query.Where(c => !c.IsArchived);
            }

            if (projectId.HasValue)
            {
                query = query.Where(c => c.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                // Title OR message-content match (content search — decision #2).
                query = query.Where(c =>
                    (c.Title != null && c.Title.ToLower().Contains(s)) ||
                    db.Messages.Any(m => m.ConversationId == c.Id &&
                                         m.ContentText != null &&
                                         m.ContentText.ToLower().Contains(s)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(c => c.LastMessageAt ?? c.DateCreated)
                .ThenByDescending(c => c.DateCreated)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });
        scope.Complete();

        // The sidebar list doesn't need per-conversation resources — build with an empty set to avoid
        // an N+1 resource load. GetByIdAsync loads them for the single-conversation views.
        return (result.items.Select(e => _factory.BuildDomain(e, [])).ToList(), result.total);
    }

    public async Task<AIConversation> CreateAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        if (conversation.Id == Guid.Empty)
        {
            conversation.Id = Guid.NewGuid();
        }

        conversation.DateCreated = conversation.DateCreated == default ? now : conversation.DateCreated;
        conversation.DateModified = now;
        conversation.Version = conversation.Version <= 0 ? 1 : conversation.Version;

        foreach (var resource in conversation.Resources)
        {
            if (resource.Id == Guid.Empty)
            {
                resource.Id = Guid.NewGuid();
            }
        }

        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            db.Conversations.Add(_factory.BuildEntity(conversation));
            foreach (var resource in conversation.Resources)
            {
                db.ConversationResources.Add(_factory.BuildResourceEntity(resource, conversation.Id));
            }

            return await db.SaveChangesAsync(cancellationToken);
        });
        scope.Complete();

        return conversation;
    }

    public async Task UpdateAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Conversations.FirstOrDefaultAsync(e => e.Id == conversation.Id, cancellationToken);
            if (entity is null)
            {
                return 0;
            }

            entity.Title = conversation.Title;
            entity.ProjectId = conversation.ProjectId;
            entity.ProfileId = conversation.ProfileId;
            entity.AgentIdOrAlias = conversation.AgentIdOrAlias;
            entity.ContextIds = _factory.BuildEntity(conversation).ContextIds;
            entity.IsPinned = conversation.IsPinned;
            entity.IsArchived = conversation.IsArchived;
            entity.DateModified = DateTime.UtcNow;
            entity.Version++;

            await ReconcileResourcesAsync(db, conversation, cancellationToken);

            return await db.SaveChangesAsync(cancellationToken);
        });
        scope.Complete();
    }

    /// <summary>
    /// Reconciles a conversation's directly-attached resources against the incoming set (add new,
    /// update existing, remove deleted) — mirrors the project repository's resource reconcile.
    /// </summary>
    private async Task ReconcileResourcesAsync(
        UmbracoAIConversationsDbContext db,
        AIConversation conversation,
        CancellationToken cancellationToken)
    {
        foreach (var resource in conversation.Resources)
        {
            if (resource.Id == Guid.Empty)
            {
                resource.Id = Guid.NewGuid();
            }
        }

        var existingResources = await db.ConversationResources
            .Where(r => r.ConversationId == conversation.Id)
            .ToListAsync(cancellationToken);

        var existingById = existingResources.ToDictionary(r => r.Id);
        var incomingIds = conversation.Resources.Select(r => r.Id).ToHashSet();

        foreach (var stale in existingResources.Where(r => !incomingIds.Contains(r.Id)))
        {
            db.ConversationResources.Remove(stale);
        }

        foreach (var resource in conversation.Resources)
        {
            if (existingById.TryGetValue(resource.Id, out var existingResource))
            {
                _factory.UpdateResourceEntity(existingResource, resource);
            }
            else
            {
                db.ConversationResources.Add(_factory.BuildResourceEntity(resource, conversation.Id));
            }
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Conversations.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity is not null)
            {
                db.Conversations.Remove(entity); // messages cascade-delete
                return await db.SaveChangesAsync(cancellationToken);
            }

            return 0;
        });
        scope.Complete();
    }

    // --- Messages ---

    public async Task<IReadOnlyList<AIMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Sequence)
                .ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(AIMessageEntityFactory.BuildDomain).ToList();
    }

    public async Task<(IReadOnlyList<AIMessage> Items, int Total)> GetMessagesPagedAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIMessageEntity> query = db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderBy(m => m.Sequence).Skip(skip).Take(take).ToListAsync(cancellationToken);
            return (items, total);
        });
        scope.Complete();
        return (result.items.Select(AIMessageEntityFactory.BuildDomain).ToList(), result.total);
    }

    public async Task AddMessagesAsync(Guid conversationId, IReadOnlyList<AIMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return;
        }

        // Server-assigned sequence with retry-on-conflict (interrogation B1): a concurrent writer may
        // grab the same next sequence, tripping the unique (ConversationId, Sequence) index. Re-read the
        // max and retry so the streamed response is never lost.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
                await scope.ExecuteWithContextAsync(async db =>
                {
                    var maxSequence = await db.Messages
                        .Where(m => m.ConversationId == conversationId)
                        .Select(m => (int?)m.Sequence)
                        .MaxAsync(cancellationToken) ?? -1;

                    var next = maxSequence + 1;
                    var now = DateTime.UtcNow;

                    foreach (var message in messages)
                    {
                        db.Messages.Add(AIMessageEntityFactory.BuildEntity(message, conversationId, next++, now));
                    }

                    var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
                    if (conversation is not null)
                    {
                        conversation.LastMessageAt = now;
                        conversation.DateModified = now;
                        conversation.Version++;
                    }

                    return await db.SaveChangesAsync(cancellationToken);
                });
                scope.Complete();
                return;
            }
            catch (DbUpdateException) when (attempt < MaxAppendRetries)
            {
                // Concurrent writer claimed our sequence — loop to re-read max and retry.
            }
        }
    }

    public async Task DeleteMessagesFromAsync(Guid conversationId, int fromSequence, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<UmbracoAIConversationsDbContext> scope = _scopeProvider.CreateScope();
        await scope.ExecuteWithContextAsync(async db =>
        {
            var toDelete = await db.Messages
                .Where(m => m.ConversationId == conversationId && m.Sequence >= fromSequence)
                .ToListAsync(cancellationToken);

            if (toDelete.Count > 0)
            {
                db.Messages.RemoveRange(toDelete);
                return await db.SaveChangesAsync(cancellationToken);
            }

            return 0;
        });
        scope.Complete();
    }
}
