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
    /// afterwards, so a caller that needs the rows doesn't load them again. When the repair fails, the stored rows
    /// are unchanged (the write is transactional) and the unrepaired rows come back.
    /// </summary>
    /// <remarks>
    /// Loading the rows is the caller's own read, so a failure there propagates. A cancelled token propagates too:
    /// it is the caller giving up, not a failed repair.
    /// </remarks>
    internal async Task<IReadOnlyList<AIMessage>> RepairRepeatedTurnsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var rows = await _repository.GetMessagesAsync(conversationId, cancellationToken);

        try
        {
            var repair = AIResentTurnMerger.RepairRepeatedTurns(rows);
            if (repair.IsEmpty)
            {
                return rows;
            }

            await _repository.RewriteMessagesAsync(conversationId, repair.RowsToUpdate, repair.IdsToDelete, cancellationToken);

            _logger.LogInformation(
                "Conversation {ConversationId}: repaired a repeated turn: removed {Deleted} stored message(s) and updated {Updated}.",
                conversationId,
                repair.IdsToDelete.Count,
                repair.RowsToUpdate.Count);

            return Apply(rows, repair);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Conversation {ConversationId}: could not repair a repeated turn; the chat is left as it was.",
                conversationId);
            return rows;
        }
    }

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
