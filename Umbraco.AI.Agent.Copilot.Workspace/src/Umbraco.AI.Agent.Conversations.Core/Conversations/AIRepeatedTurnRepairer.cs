using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Repairs a turn an old chat already holds twice, when the chat is read. It loads the stored rows, asks
/// <see cref="AIResentTurnMerger"/> what to change, and writes that in one transaction. The I/O lives here so the
/// merger stays pure. A repair never fails a read: on any error it logs a warning and the read goes on with the
/// unrepaired rows. A chat with no repeat is not written to and nothing is logged.
/// </summary>
/// <remarks>
/// It reaches <see cref="IAIConversationRepository"/> directly, like <see cref="ConversationChatHistoryProvider"/>
/// does, so it is not a second service over the same repository: both read paths call it after their own checks
/// (the service after the ownership check), and it holds no state.
/// </remarks>
internal sealed class AIRepeatedTurnRepairer
{
    private readonly IAIConversationRepository _repository;
    private readonly ILogger<AIRepeatedTurnRepairer> _logger;

    public AIRepeatedTurnRepairer(IAIConversationRepository repository, ILogger<AIRepeatedTurnRepairer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Merges any repeated turn in the conversation's stored rows and returns the rows as the store holds them
    /// afterwards, with each readable row already deserialized, so a caller that needs the rows or their messages
    /// doesn't load or read them again. When the repair fails, the stored rows are unchanged (the write is
    /// transactional) and the unrepaired rows come back.
    /// </summary>
    /// <remarks>
    /// Loading the rows is the caller's own read, so a failure there propagates. A cancelled token propagates too:
    /// it is the caller giving up, not a failed repair.
    /// </remarks>
    internal async Task<RepairedHistory> RepairRepeatedTurnsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var rows = await _repository.GetMessagesAsync(conversationId, cancellationToken);
        var readable = AIStoredMessageMapper.DeserializeReadable(rows);

        try
        {
            var repair = AIResentTurnMerger.RepairRepeatedTurns(rows, readable);
            if (repair.IsEmpty)
            {
                return new RepairedHistory(rows, readable);
            }

            await _repository.RewriteMessagesAsync(conversationId, repair.RowsToUpdate, repair.IdsToDelete, cancellationToken);

            _logger.LogInformation(
                "Conversation {ConversationId}: repaired a repeated turn: removed {Deleted} stored message(s) and updated {Updated}.",
                conversationId,
                repair.IdsToDelete.Count,
                repair.RowsToUpdate.Count);

            // A repeat is rare and fixed once, so the rows that are left are simply read again.
            var repaired = Apply(rows, repair);
            return new RepairedHistory(repaired, AIStoredMessageMapper.DeserializeReadable(repaired));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Conversation {ConversationId}: could not repair a repeated turn; the chat is left as it was.",
                conversationId);
            return new RepairedHistory(rows, readable);
        }
    }

    /// <summary>
    /// The conversation's rows in sequence order, and the messages that could be read from them, each paired with
    /// its index in <see cref="Rows"/>.
    /// </summary>
    internal sealed record RepairedHistory(
        IReadOnlyList<AIMessage> Rows,
        IReadOnlyList<(int Row, ChatMessage Message)> Readable);

    private static List<AIMessage> Apply(IReadOnlyList<AIMessage> rows, AIResentTurnMerger.RepeatedTurnRepair repair)
    {
        var deleted = repair.IdsToDelete.ToHashSet();
        var updated = repair.RowsToUpdate.ToDictionary(r => r.Id);

        return rows
            .Where(r => !deleted.Contains(r.Id))
            .Select(r => updated.TryGetValue(r.Id, out var update) ? update : r)
            .ToList();
    }
}
