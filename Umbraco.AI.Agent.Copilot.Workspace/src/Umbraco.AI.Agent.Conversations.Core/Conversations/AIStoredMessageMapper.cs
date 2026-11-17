using System.Text.Json;

using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// The one place that knows how a <see cref="ChatMessage"/> is written to, and read back from, an
/// <see cref="AIMessage"/> row's <see cref="AIMessage.ContentJson"/> and <see cref="AIMessage.ContentText"/>.
/// </summary>
internal static class AIStoredMessageMapper
{
    /// <summary>The serialized form stored in <see cref="AIMessage.ContentJson"/>.</summary>
    internal static string Serialize(ChatMessage message)
        => JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions);

    /// <summary>
    /// Reads a stored message back, or null when it can't be re-materialized (e.g. a custom
    /// <see cref="AIContent"/> type that was later uninstalled).
    /// </summary>
    internal static ChatMessage? TryDeserialize(string contentJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatMessage>(contentJson, AIJsonUtilities.DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes each stored row once, keeping the row index beside each message that could be read (a row
    /// that can't be read is left out). Callers that make several passes over the same rows share the result.
    /// </summary>
    internal static List<(int Row, ChatMessage Message)> DeserializeReadable(IReadOnlyList<AIMessage> stored)
    {
        var readable = new List<(int Row, ChatMessage Message)>(stored.Count);
        for (var i = 0; i < stored.Count; i++)
        {
            if (TryDeserialize(stored[i].ContentJson) is { } message)
            {
                readable.Add((i, message));
            }
        }

        return readable;
    }

    /// <summary>A copy of <paramref name="row"/> that holds <paramref name="message"/> as its content.</summary>
    internal static AIMessage WithMessage(AIMessage row, ChatMessage message)
        => new()
        {
            Id = row.Id,
            ConversationId = row.ConversationId,
            Sequence = row.Sequence,
            Role = row.Role,
            AgentId = row.AgentId,
            ContentJson = Serialize(message),
            ContentText = message.Text,
            SchemaVersion = row.SchemaVersion,
            InputTokens = row.InputTokens,
            OutputTokens = row.OutputTokens,
            DateCreated = row.DateCreated,
        };

    /// <summary>A copy of <paramref name="message"/> with the same metadata and the given contents.</summary>
    internal static ChatMessage WithContents(ChatMessage message, IList<AIContent> contents)
        => new(message.Role, contents)
        {
            AuthorName = message.AuthorName,
            CreatedAt = message.CreatedAt,
            MessageId = message.MessageId,
            AdditionalProperties = message.AdditionalProperties,
            RawRepresentation = message.RawRepresentation,
        };
}
