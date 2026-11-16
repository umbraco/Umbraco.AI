using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>Builders shared by the specs that feed <see cref="AIResentTurnMerger"/>.</summary>
internal static class ResentTurnFixtures
{
    /// <summary>The repairer over <paramref name="repository"/>, with a logger that discards.</summary>
    internal static AIRepeatedTurnRepairer Repairer(IAIConversationRepository repository)
        => new(repository, NullLogger<AIRepeatedTurnRepairer>.Instance);

    internal const string StandIn = AIResentTurnMerger.StandInResultText;

    internal static ChatMessage User(string id, string text = "Hello")
        => new(ChatRole.User, text) { MessageId = id };

    internal static ChatMessage Call(string id, params string[] callIds)
        => new(ChatRole.Assistant, callIds.Select(c => (AIContent)new FunctionCallContent(c, "tool")).ToList()) { MessageId = id };

    internal static ChatMessage Result(string id, params (string CallId, string Result)[] results)
        => new(ChatRole.Tool, results.Select(r => (AIContent)new FunctionResultContent(r.CallId, r.Result)).ToList()) { MessageId = id };

    /// <summary>A stored row, serialized the way the history provider persists a message.</summary>
    internal static AIMessage Row(ChatMessage message, int sequence)
        => new()
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.Empty,
            Sequence = sequence,
            Role = message.Role.Value,
            ContentJson = JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions),
            ContentText = message.Text,
            SchemaVersion = 2,
        };

    internal static List<AIMessage> Rows(params ChatMessage[] messages)
        => messages.Select((m, i) => Row(m, i + 1)).ToList();

    /// <summary>The rows as they are left in the store once the repair is written.</summary>
    internal static List<AIMessage> ApplyRows(IReadOnlyList<AIMessage> rows, AIResentTurnMerger.RepeatedTurnRepair repair)
        => rows
            .Where(r => !repair.IdsToDelete.Contains(r.Id))
            .Select(r => repair.RowsToUpdate.FirstOrDefault(u => u.Id == r.Id) ?? r)
            .ToList();

    internal static List<ChatMessage> Apply(IReadOnlyList<AIMessage> rows, AIResentTurnMerger.RepeatedTurnRepair repair)
        => ApplyRows(rows, repair)
            .Select(r => JsonSerializer.Deserialize<ChatMessage>(r.ContentJson, AIJsonUtilities.DefaultOptions)!)
            .ToList();

    /// <summary>The updated rows of a merge, as <c>row index, text</c> pairs against the stored rows.</summary>
    internal static List<(int Row, string Text)> Updated(IReadOnlyList<AIMessage> rows, AIResentTurnMerger.ResentTurnResult result)
        => result.RowsToUpdate
            .Select(u => (rows.ToList().FindIndex(r => r.Id == u.Id), Describe(JsonSerializer.Deserialize<ChatMessage>(u.ContentJson, AIJsonUtilities.DefaultOptions)!)))
            .ToList();

    /// <summary>A short text per message, e.g. <c>user:u1</c>, <c>call:C</c>, <c>result:C=real</c>.</summary>
    internal static string Describe(ChatMessage message)
        => string.Join("+", message.Contents.Select(c => c switch
        {
            FunctionCallContent call => $"call:{call.CallId}",
            FunctionResultContent result => $"result:{result.CallId}={result.Result}",
            _ => $"{message.Role.Value}:{message.MessageId}",
        }));
}
