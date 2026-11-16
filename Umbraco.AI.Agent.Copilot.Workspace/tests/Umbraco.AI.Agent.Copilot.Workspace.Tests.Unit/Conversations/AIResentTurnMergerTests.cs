using Microsoft.Extensions.AI;

using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;

using Xunit;

using static Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Edges of the merge rules that the story specs don't spell out (R1, R2, R4, R6, R9), and how a
/// stored stand-in is recognised after a JSON round trip.
/// </summary>
public class AIResentTurnMergerTests
{
    private static AIMessage Unreadable(int sequence)
        => new()
        {
            Id = Guid.NewGuid(),
            Sequence = sequence,
            Role = "assistant",
            ContentJson = "{ not json",
            ContentText = string.Empty,
            SchemaVersion = 2,
        };

    // R4 - a result whose call already has a stored real result is dropped and the stored one is kept
    [Fact]
    public void ResentResult_WhenStoredResultIsReal_IsDroppedWithoutAnUpdate()
    {
        var stored = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "stored-real")));
        List<ChatMessage> incoming = [User("u1"), Call("b1", "C"), Result("b2", ("C", "other"))];

        var result = AIResentTurnMerger.MergeResentTurn(stored, incoming);

        result.RowsToUpdate.ShouldBeEmpty();
    }

    // R2 - an assistant message is only dropped when every call id is already stored
    [Fact]
    public void ResentAssistantMessage_WithAnUnstoredCall_IsPassedOn()
    {
        var stored = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)));
        List<ChatMessage> incoming = [User("u1"), Call("b1", "C", "NEW")];

        var result = AIResentTurnMerger.MergeResentTurn(stored, incoming);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["b1"]);
    }

    // A stored stand-in is recognised after it has been through JSON (the result comes back as a JsonElement)
    [Fact]
    public void StandInResult_ReadBackFromTheStore_IsStillReplaced()
    {
        var stored = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)));

        var result = AIResentTurnMerger.MergeResentTurn(stored, [User("u1"), Result("b1", ("C", "real"))]);

        Updated(stored, result).ShouldBe([(2, "result:C=real")]);
    }

    // R2 - a turn resent a third time, after the stand-in was already replaced, changes nothing more
    [Fact]
    public void TurnResentAThirdTime_AfterTheStandInWasReplaced_DropsEverything()
    {
        var stored = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real")));
        List<ChatMessage> incoming = [User("u1"), Call("b1", "C"), Result("b2", ("C", "real"))];

        var result = AIResentTurnMerger.MergeResentTurn(stored, incoming);

        result.Candidates.ShouldBeEmpty();
    }

    // R4 - a resent tool message that holds one stored result and one new result keeps only the new one
    [Fact]
    public void ResentToolMessage_WithOneStoredAndOneNewResult_IsTrimmedToTheNewResult()
    {
        var stored = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real")));
        List<ChatMessage> incoming = [User("u1"), Result("b1", ("C", "real"), ("D", "fresh"))];

        var result = AIResentTurnMerger.MergeResentTurn(stored, incoming);

        result.Candidates.Select(Describe).ShouldBe(["result:D=fresh"]);
    }

    // R1 - a last stored user message without an id can't be matched, so nothing is merged
    [Fact]
    public void StoredUserMessageWithoutAnId_PassesEveryCandidateOn()
    {
        var stored = Rows(new ChatMessage(ChatRole.User, "Hello"), Call("a1", "C"));
        List<ChatMessage> incoming = [User("u1"), Call("b1", "C")];

        var result = AIResentTurnMerger.MergeResentTurn(stored, incoming);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["u1", "b1"]);
    }

    // A stored row that can't be read is skipped for matching and the update still lands on the right row
    [Fact]
    public void UnreadableStoredRow_IsSkippedAndTheUpdateAddressesTheRightRow()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)));
        rows.Insert(1, Unreadable(99));

        var result = AIResentTurnMerger.MergeResentTurn(rows, [User("u1"), Result("b1", ("C", "real"))]);

        Updated(rows, result).ShouldBe([(3, "result:C=real")]);
    }

    // R6 - the repeated block's rows are the ones deleted, the stand-in row is the one updated
    [Fact]
    public void RepeatedBlock_DeletesTheLaterRowsAndUpdatesTheStandIn()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)), User("u1"), Call("a2", "C"), Result("t2", ("C", "real")));

        var repair = AIResentTurnMerger.RepairRepeatedTurns(rows);

        // Row numbers, so the one assertion names which rows went and which was rewritten.
        var deleted = repair.IdsToDelete.Select(id => "delete:" + rows.FindIndex(r => r.Id == id));
        var updated = repair.RowsToUpdate.Select(row => "update:" + rows.FindIndex(r => r.Id == row.Id));

        deleted.Concat(updated).ShouldBe(["delete:3", "delete:4", "delete:5", "update:2"]);
    }

    // R6 - a model reply stored after the repeated block stays where it is
    [Fact]
    public void RepeatedBlock_KeepsALaterModelReply()
    {
        var rows = Rows(
            User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)),
            User("u1"), Call("a2", "C"), Result("t2", ("C", "real")),
            new ChatMessage(ChatRole.Assistant, "Done") { MessageId = "a3" });

        var remaining = Apply(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        remaining.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real", "assistant:a3"]);
    }

    // R6 - a repeated user id with no repeated call is not a repeat, so nothing is repaired
    [Fact]
    public void RepeatedUserId_WithoutARepeatedCall_IsNotRepaired()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "done")), User("u1"), Call("a2", "OTHER"), Result("t2", ("OTHER", "done")));

        AIResentTurnMerger.RepairRepeatedTurns(rows).IsEmpty.ShouldBeTrue();
    }

    // R6 - a turn stored three times is repaired down to one
    [Fact]
    public void TurnStoredThreeTimes_IsRepairedToOneCopy()
    {
        var rows = Rows(
            User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)),
            User("u1"), Call("a2", "C"), Result("t2", ("C", StandIn)),
            User("u1"), Call("a3", "C"), Result("t3", ("C", "real")));

        var remaining = Apply(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        remaining.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // R6 - a stored row that can't be read is left alone when repairing
    [Fact]
    public void RepairWithAnUnreadableRow_LeavesThatRowAlone()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", StandIn)), User("u1"), Call("a2", "C"), Result("t2", ("C", "real")));
        var bad = Unreadable(99);
        rows.Insert(2, bad);

        var repair = AIResentTurnMerger.RepairRepeatedTurns(rows);

        repair.IdsToDelete.ShouldNotContain(bad.Id);
    }

    // R9 - two user messages with the same text but different ids are two turns
    [Fact]
    public void SameTextTwiceWithDifferentIds_IsNotRepaired()
    {
        var rows = Rows(User("u1", "Hello"), new ChatMessage(ChatRole.Assistant, "Hi") { MessageId = "a1" }, User("u2", "Hello"));

        AIResentTurnMerger.RepairRepeatedTurns(rows).IsEmpty.ShouldBeTrue();
    }
}
