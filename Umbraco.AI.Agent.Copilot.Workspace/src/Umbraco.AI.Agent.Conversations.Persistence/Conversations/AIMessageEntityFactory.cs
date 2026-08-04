using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// Converts between <see cref="AIMessage"/> and <see cref="AIMessageEntity"/>.
/// </summary>
internal static class AIMessageEntityFactory
{
    public static AIMessage BuildDomain(AIMessageEntity entity) => new()
    {
        Id = entity.Id,
        ConversationId = entity.ConversationId,
        Sequence = entity.Sequence,
        Role = entity.Role,
        ContentJson = entity.ContentJson,
        ContentText = entity.ContentText,
        SchemaVersion = entity.SchemaVersion,
        InputTokens = entity.InputTokens,
        OutputTokens = entity.OutputTokens,
        DateCreated = entity.DateCreated,
    };

    /// <summary>
    /// Builds an entity for insertion, stamping the server-assigned <paramref name="sequence"/> and
    /// <paramref name="dateCreated"/>. The message id is preserved when supplied (stable correlation) and
    /// generated when empty.
    /// </summary>
    public static AIMessageEntity BuildEntity(AIMessage domain, Guid conversationId, int sequence, DateTime dateCreated) => new()
    {
        Id = domain.Id == Guid.Empty ? Guid.NewGuid() : domain.Id,
        ConversationId = conversationId,
        Sequence = sequence,
        Role = domain.Role,
        ContentJson = domain.ContentJson,
        ContentText = domain.ContentText,
        SchemaVersion = domain.SchemaVersion <= 0 ? 1 : domain.SchemaVersion,
        InputTokens = domain.InputTokens,
        OutputTokens = domain.OutputTokens,
        DateCreated = dateCreated,
    };
}
