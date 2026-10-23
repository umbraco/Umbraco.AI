using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// Converts between <see cref="AIConversationShare"/> and <see cref="AIConversationShareEntity"/>. No
/// JSON serialization or resource-type lookups are involved (unlike
/// <see cref="AIConversationEntityFactory"/>'s resource mapping), so — mirroring
/// <see cref="AIMessageEntityFactory"/>'s equally trivial mapping — this is a plain static class
/// rather than one requiring DI-resolved collaborators.
/// </summary>
internal static class AIConversationShareEntityFactory
{
    public static AIConversationShare BuildDomain(AIConversationShareEntity entity) => new()
    {
        Id = entity.Id,
        ConversationId = entity.ConversationId,
        SharedWithUserKey = entity.SharedWithUserKey,
        SharedByUserKey = entity.SharedByUserKey,
        ScopeMode = (AIConversationShareScopeMode)entity.ScopeMode,
        CutoffMessageId = entity.CutoffMessageId,
        DateShared = entity.DateShared,
        DateRevoked = entity.DateRevoked,
    };

    /// <summary>
    /// Builds a brand-new row for an insert. <paramref name="id"/> and <paramref name="dateShared"/>
    /// are server-assigned by the caller rather than read off <paramref name="domain"/> — a caller's
    /// <see cref="AIConversationShare"/> may be unpopulated, stale, or (for <c>Id</c>) reused from an
    /// earlier revoked share, none of which should ever land in a freshly inserted row. A new grant is
    /// always active, so <see cref="AIConversationShareEntity.DateRevoked"/> is forced to <c>null</c>
    /// regardless of what <paramref name="domain"/> carried.
    /// </summary>
    public static AIConversationShareEntity BuildEntity(AIConversationShare domain, Guid id, DateTime dateShared) => new()
    {
        Id = id,
        ConversationId = domain.ConversationId,
        SharedWithUserKey = domain.SharedWithUserKey,
        SharedByUserKey = domain.SharedByUserKey,
        ScopeMode = (int)domain.ScopeMode,
        CutoffMessageId = domain.CutoffMessageId,
        DateShared = dateShared,
        DateRevoked = null,
    };
}
