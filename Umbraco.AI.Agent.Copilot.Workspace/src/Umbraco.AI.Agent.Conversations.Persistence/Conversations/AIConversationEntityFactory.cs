using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// Converts between <see cref="AIConversation"/> and <see cref="AIConversationEntity"/>.
/// </summary>
internal static class AIConversationEntityFactory
{
    public static AIConversation BuildDomain(AIConversationEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Title = entity.Title,
        UserKey = entity.UserKey,
        AgentIdOrAlias = entity.AgentIdOrAlias,
        ProfileId = entity.ProfileId,
        IsPinned = entity.IsPinned,
        IsArchived = entity.IsArchived,
        DateCreated = entity.DateCreated,
        DateModified = entity.DateModified,
        LastMessageAt = entity.LastMessageAt,
        Version = entity.Version,
    };

    public static AIConversationEntity BuildEntity(AIConversation domain) => new()
    {
        Id = domain.Id,
        ProjectId = domain.ProjectId,
        Title = domain.Title,
        UserKey = domain.UserKey,
        AgentIdOrAlias = domain.AgentIdOrAlias,
        ProfileId = domain.ProfileId,
        IsPinned = domain.IsPinned,
        IsArchived = domain.IsArchived,
        DateCreated = domain.DateCreated,
        DateModified = domain.DateModified,
        LastMessageAt = domain.LastMessageAt,
        Version = domain.Version,
    };
}
