// S1 - A frontend-tool turn is stored once (AC1-AC8)
using System.Text.Json;

using Microsoft.Extensions.AI;

using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;

using Xunit;

using static Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: a turn where the agent used a browser (frontend) tool is stored once, with the real tool result.
/// AC1-AC7 run against <see cref="AIResentTurnMerger"/> directly (T2). The provider-level specs (what the model
/// receives and what the store holds after a run) are added by T3; AC8 stays pending for T3.
/// </summary>
public class FrontendToolTurnStoredOnceTests
{
    private const string Pending = "Pending: T3";

    // The stored turn from AC1: U, call C, the stand-in for C. The browser resends U, its copy of C and the real result.
    private static List<AIMessage> StoredTurn() => Rows(User("u1"), Call("srv-a", "C"), Result("srv-t", ("C", StandIn)));

    private static List<ChatMessage> ResentTurn() => [User("u1"), Call("br-a", "C"), Result("br-t", ("C", "real"))];

    private static AIResentTurnMerger.ResentTurnResult Merge(List<AIMessage> stored, List<ChatMessage> incoming)
        => AIResentTurnMerger.MergeResentTurn(stored, incoming);

    // Scenario: the browser resends a turn the store already holds (happy path)
    // Given (AC1-AC6): the store holds user message U, tool call C and the stand-in result for C;
    // the browser resends U (same id), its copy of call C and the real result for C.
    // AC5 and AC6 start from their own stores, noted on each.

    // AC1 - Then U is not passed to the model a second time
    [Fact]
    public void ResentTurn_WhenUserMessageResent_DropsItFromWhatTheModelGets()
    {
        var result = Merge(StoredTurn(), ResentTurn());

        result.Candidates.Any(m => m.Role == ChatRole.User).ShouldBeFalse();
    }

    // AC2 - Then the browser's assistant message with call C is not passed to the model
    [Fact]
    public void ResentTurn_WhenToolCallResent_DropsTheBrowsersAssistantMessage()
    {
        var result = Merge(StoredTurn(), ResentTurn());

        result.Candidates.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ShouldBeEmpty();
    }

    // AC3 - Then the stored result for call C holds the real result, not "Success: Function completed."
    [Fact]
    public void ResentTurn_WhenRealResultArrives_ReplacesTheStandInInTheStore()
    {
        var stored = StoredTurn();
        var result = Merge(stored, ResentTurn());

        Updated(stored, result).ShouldBe([(2, "result:C=real")]);
    }

    // AC4 - Then the conversation stores U once, call C once and one result for C
    [Fact]
    public void ResentTurn_WhenRunFinishes_StoresUserMessageCallAndResultOnce()
    {
        var stored = StoredTurn();
        var result = Merge(stored, ResentTurn());

        // What the store holds after the merge: the stored messages with the updates written, then whatever is passed on.
        var afterRun = stored
            .Select(r => result.RowsToUpdate.FirstOrDefault(u => u.Id == r.Id) ?? r)
            .Select(r => JsonSerializer.Deserialize<ChatMessage>(r.ContentJson, AIJsonUtilities.DefaultOptions)!)
            .ToList();

        afterRun.AddRange(result.Candidates);

        afterRun.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC5 - Given the store holds U, server call S with its real result, frontend call F and the stand-in for F;
    // the browser resends U, calls S and F, and the real results for S and F.
    // Then the stored result for S is unchanged and the stored result for F holds the real result
    [Fact]
    public void ResentTurn_WithServerToolBeforeFrontendTool_LeavesServerResultUnchangedAndUpdatesFrontendResult()
    {
        var stored = Rows(
            User("u1"),
            Call("srv-a1", "S"),
            Result("srv-t1", ("S", "server-result")),
            Call("srv-a2", "F"),
            Result("srv-t2", ("F", StandIn)));
        List<ChatMessage> incoming =
        [
            User("u1"),
            Call("br-a", "S", "F"),
            Result("br-t", ("S", "server-result-again"), ("F", "ui-result")),
        ];

        var result = Merge(stored, incoming);

        // Only the stand-in for F is rewritten (stored index 4); the stored result for S is not touched.
        Updated(stored, result).ShouldBe([(4, "result:F=ui-result")]);
    }

    // AC6 - When the resent turn is followed by a new user message N, then N is passed to the model and stored
    [Fact]
    public void ResentTurn_FollowedByNewUserMessage_PassesAndStoresTheNewMessage()
    {
        var result = Merge(StoredTurn(), [.. ResentTurn(), User("n1", "Next question")]);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["n1"]);
    }

    // Scenario: sad path and edges

    // AC7 - Given the store holds a finished turn starting with user message U1 "Hello"; when a new run sends U2 "Hello" with a different id,
    // then U2 is passed to the model and stored as a new turn
    [Fact]
    public void SameTextWithDifferentId_IsTreatedAsANewTurn()
    {
        List<ChatMessage> incoming = [User("u2", "Hello")];

        var result = Merge(Rows(User("u1", "Hello"), Call("a", "C"), Result("t", ("C", "done"))), incoming);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["u2"]);
    }

    // AC8 (T3: provider/stream level) - Given a run paused for an approval; when the browser resumes it with an AG-UI resume entry,
    // then the run takes the existing approval path and the merge is not applied
    [Fact(Skip = Pending)]
    public void ApprovalResume_TakesTheExistingApprovalPathWithoutTheMerge()
        => Assert.Fail(Pending);
}
