using System.Text.Json;

using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Pure merge rules for a turn that is stored twice: a browser that resends a whole turn after a frontend
/// tool, or an old chat that already holds the repeat. The stored copy of the user message and tool call is
/// kept; a real tool result replaces the stored stand-in; everything else resent is dropped. No I/O and no
/// dependencies, so the caller decides what to do with the answer (the history provider passes the candidates
/// on and writes the updates; the repair on read writes the rows).
/// </summary>
internal static class AIResentTurnMerger
{
    /// <summary>
    /// The text a tool result carries when the tool returned nothing. It comes from Microsoft.Extensions.AI's
    /// empty-result text (<c>FunctionInvokingChatClient</c>, Microsoft.Extensions.AI.dll 10.7.0). Frontend tools
    /// return nothing on purpose, and chats stored before the fix already hold this text, so it can't be a marker of our own.
    /// </summary>
    internal const string StandInResultText = "Success: Function completed.";

    /// <summary>
    /// Merges the <paramref name="candidates"/> a plain run arrived with into the <paramref name="stored"/> rows.
    /// <paramref name="readable"/> pairs each row that could be read (see <see cref="AIStoredMessageMapper.DeserializeReadable"/>)
    /// with its index in <paramref name="stored"/>, so a caller that needs the messages for another pass reads the rows once.
    /// The anchor is the first candidate user message whose id equals the last stored user message's. Without an
    /// anchor the candidates come back untouched. A stored row that can't be read is skipped, never rewritten.
    /// </summary>
    internal static ResentTurnResult MergeResentTurn(
        IReadOnlyList<AIMessage> stored,
        IReadOnlyList<(int Row, ChatMessage Message)> readable,
        IReadOnlyList<ChatMessage> candidates)
    {
        var storedUserIndex = LastUserIndexWithId(readable);
        if (storedUserIndex < 0)
        {
            return new ResentTurnResult(candidates, []);
        }

        var anchorId = readable[storedUserIndex].Message.MessageId;
        var anchorIndex = -1;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Role == ChatRole.User && string.Equals(candidates[i].MessageId, anchorId, StringComparison.Ordinal))
            {
                anchorIndex = i;
                break;
            }
        }

        if (anchorIndex < 0)
        {
            return new ResentTurnResult(candidates, []);
        }

        var storedTurn = readable.Skip(storedUserIndex).ToList();
        var resent = candidates.Skip(anchorIndex).ToList();
        var merge = Apply(storedTurn.Select(t => t.Message).ToList(), resent);

        var passedOn = new List<ChatMessage>(candidates.Count);
        passedOn.AddRange(candidates.Take(anchorIndex));
        passedOn.AddRange(merge.Outcomes.OfType<ChatMessage>());

        var rowsToUpdate = merge.Updates
            .Select(u => (Row: storedTurn[u.Key].Row, Message: u.Value))
            .OrderBy(u => u.Row)
            .Select(u => AIStoredMessageMapper.WithMessage(stored[u.Row], u.Message))
            .ToList();

        return new ResentTurnResult(passedOn, rowsToUpdate);
    }

    /// <summary>
    /// Finds turns already stored twice (a user message whose id appeared earlier, followed by calls and results
    /// that appeared earlier) and returns the rows to update and the row ids to delete. A history with no repeat
    /// gives an empty answer, so a repaired history is left alone on the next read. <paramref name="readable"/>
    /// is the <paramref name="stored"/> rows already deserialized (see <see cref="AIStoredMessageMapper.DeserializeReadable"/>).
    /// </summary>
    internal static RepeatedTurnRepair RepairRepeatedTurns(
        IReadOnlyList<AIMessage> stored,
        IReadOnlyList<(int Row, ChatMessage Message)> readable)
    {
        var messages = new ChatMessage?[stored.Count];
        foreach (var (row, readableMessage) in readable)
        {
            messages[row] = readableMessage;
        }

        var firstUserRowById = new Dictionary<string, int>(StringComparer.Ordinal);
        var deleted = new HashSet<int>();
        var updated = new Dictionary<int, ChatMessage>();

        for (var j = 0; j < messages.Length; j++)
        {
            var message = messages[j];
            if (message is null || deleted.Contains(j) || message.Role != ChatRole.User || string.IsNullOrEmpty(message.MessageId))
            {
                continue;
            }

            if (!firstUserRowById.TryGetValue(message.MessageId, out var firstRow))
            {
                firstUserRowById[message.MessageId] = j;
                continue;
            }

            var blockEnd = NextUserRow(messages, j + 1);

            var storedRows = Rows(messages, firstRow, j, deleted, updated);
            var blockRows = Rows(messages, j, blockEnd, deleted, updated);

            // R6: only a user id followed by calls already stored earlier is a repeat; otherwise leave the block alone.
            var earlierCallIds = CallIds(storedRows.Select(r => r.Message));
            if (!CallIds(blockRows.Select(r => r.Message)).Overlaps(earlierCallIds))
            {
                continue;
            }

            var merge = Apply(
                storedRows.Select(r => r.Message).ToList(),
                blockRows.Select(r => r.Message).ToList());

            for (var b = 0; b < blockRows.Count; b++)
            {
                var outcome = merge.Outcomes[b];
                if (outcome is null)
                {
                    deleted.Add(blockRows[b].Row);
                }
                else if (!ReferenceEquals(outcome, blockRows[b].Message))
                {
                    updated[blockRows[b].Row] = outcome;
                }
            }

            foreach (var update in merge.Updates)
            {
                updated[storedRows[update.Key].Row] = update.Value;
            }
        }

        var rowsToUpdate = updated
            .Where(u => !deleted.Contains(u.Key))
            .OrderBy(u => u.Key)
            .Select(u => AIStoredMessageMapper.WithMessage(stored[u.Key], u.Value))
            .ToList();
        var idsToDelete = deleted.OrderBy(r => r).Select(r => stored[r].Id).ToList();

        return new RepeatedTurnRepair(rowsToUpdate, idsToDelete);
    }

    private static MergeOutcome Apply(IReadOnlyList<ChatMessage> storedTurn, IReadOnlyList<ChatMessage> resent)
    {
        var storedCallIds = new HashSet<string>(StringComparer.Ordinal);
        var storedResults = new Dictionary<string, StoredResult>(StringComparer.Ordinal);
        for (var s = 0; s < storedTurn.Count; s++)
        {
            foreach (var content in storedTurn[s].Contents)
            {
                switch (content)
                {
                    case FunctionCallContent call:
                        storedCallIds.Add(call.CallId);
                        break;
                    case FunctionResultContent result:
                        storedResults[result.CallId] = new StoredResult(s, IsStandIn(result));
                        break;
                }
            }
        }

        var outcomes = new ChatMessage?[resent.Count];
        var updates = new Dictionary<int, ChatMessage>();

        // resent[0] is the anchor user message: the stored copy stays, so it is dropped (outcome null).
        for (var r = 1; r < resent.Count; r++)
        {
            var message = resent[r];

            if (message.Role == ChatRole.Assistant)
            {
                // Deliberate (R2): an assistant message is only dropped when every call id is already stored, so text-only replies pass through.
                var callIds = message.Contents.OfType<FunctionCallContent>().Select(c => c.CallId).ToList();
                outcomes[r] = callIds.Count > 0 && callIds.All(storedCallIds.Contains) ? null : message;
                continue;
            }

            var kept = new List<AIContent>(message.Contents.Count);
            foreach (var content in message.Contents)
            {
                if (content is not FunctionResultContent result || !storedResults.TryGetValue(result.CallId, out var storedResult))
                {
                    kept.Add(content);
                    continue;
                }

                if (storedResult.IsStandIn && !IsStandIn(result))
                {
                    var storedIndex = storedResult.StoredIndex;
                    var current = updates.TryGetValue(storedIndex, out var already) ? already : storedTurn[storedIndex];
                    updates[storedIndex] = WithResult(current, result);
                    storedResults[result.CallId] = storedResult with { IsStandIn = false };
                }
            }

            outcomes[r] = kept.Count == message.Contents.Count
                ? message
                : kept.Count == 0 ? null : AIStoredMessageMapper.WithContents(message, kept);
        }

        return new MergeOutcome(outcomes, updates);
    }

    private static ChatMessage WithResult(ChatMessage stored, FunctionResultContent replacement)
    {
        var contents = stored.Contents
            .Select(c => c is FunctionResultContent existing && existing.CallId == replacement.CallId ? replacement : c)
            .ToList();
        return AIStoredMessageMapper.WithContents(stored, contents);
    }

    private static bool IsStandIn(FunctionResultContent result)
        => result.Result switch
        {
            string text => text == StandInResultText,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() == StandInResultText,
            _ => false,
        };

    private static int LastUserIndexWithId(IReadOnlyList<(int Row, ChatMessage Message)> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].Message.Role == ChatRole.User)
            {
                return string.IsNullOrEmpty(messages[i].Message.MessageId) ? -1 : i;
            }
        }

        return -1;
    }

    private static HashSet<string> CallIds(IEnumerable<ChatMessage> messages)
        => messages
            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
            .Select(c => c.CallId)
            .ToHashSet(StringComparer.Ordinal);

    private static int NextUserRow(IReadOnlyList<ChatMessage?> messages, int from)
    {
        for (var i = from; i < messages.Count; i++)
        {
            if (messages[i]?.Role == ChatRole.User)
            {
                return i;
            }
        }

        return messages.Count;
    }

    private static List<(int Row, ChatMessage Message)> Rows(
        IReadOnlyList<ChatMessage?> messages,
        int from,
        int to,
        HashSet<int> deleted,
        Dictionary<int, ChatMessage> updated)
    {
        var rows = new List<(int Row, ChatMessage Message)>();
        for (var i = from; i < to; i++)
        {
            if (messages[i] is { } message && !deleted.Contains(i))
            {
                rows.Add((i, updated.TryGetValue(i, out var current) ? current : message));
            }
        }

        return rows;
    }

    private sealed record StoredResult(int StoredIndex, bool IsStandIn);

    /// <summary>Per resent message: null when dropped, the same instance when untouched, a copy when trimmed.</summary>
    private sealed record MergeOutcome(IReadOnlyList<ChatMessage?> Outcomes, IReadOnlyDictionary<int, ChatMessage> Updates);

    /// <summary>The candidates to pass on to the model, and the stored rows to update (new content, same row id).</summary>
    internal sealed record ResentTurnResult(
        IReadOnlyList<ChatMessage> Candidates,
        IReadOnlyList<AIMessage> RowsToUpdate);

    /// <summary>The rows to update (new content) and the ids of the rows to delete.</summary>
    internal sealed record RepeatedTurnRepair(
        IReadOnlyList<AIMessage> RowsToUpdate,
        IReadOnlyList<Guid> IdsToDelete)
    {
        /// <summary>True when the history held no repeated turn.</summary>
        internal bool IsEmpty => RowsToUpdate.Count == 0 && IdsToDelete.Count == 0;
    }
}
