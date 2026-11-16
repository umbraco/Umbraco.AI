// S2 - An old chat with a repeated turn repairs itself (AC1-AC6)
using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;

using Xunit;

using static Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: an old chat that already holds a repeated turn repairs itself when read.
/// AC1, AC4 and AC5 run against <see cref="AIResentTurnMerger"/> directly (T2). T4 adds the specs through the
/// service and provider (read for display, load for a run, no write, failed repair); AC2, AC3 and AC6 stay pending for T4.
/// </summary>
public class OldChatRepeatedTurnRepairTests
{
    private const string Pending = "Pending: T4";

    // The stored rows from AC1: U, call C, the stand-in for C, then U again, call C again and the real result for C.
    private static List<AIMessage> RepeatedRows() =>
        Rows(
            User("u1"),
            Call("a1", "C"),
            Result("t1", ("C", StandIn)),
            User("u1"),
            Call("a2", "C"),
            Result("t2", ("C", "real")));

    // Scenario: a chat holding a repeated turn (happy path)
    // Given (AC1-AC3): the store holds U, call C, the stand-in for C, then U again, call C again and the real result for C.

    // AC1 (merger level) - Then the returned messages hold U once, call C once and the real result for C
    [Fact]
    public void ReadForDisplay_ReturnsTheRepeatedTurnOnce()
    {
        var rows = RepeatedRows();

        var remaining = Apply(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        remaining.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC2 - Then the model gets U once, call C once and the real result for C
    [Fact(Skip = Pending)]
    public void LoadForRun_SendsTheRepeatedTurnToTheModelOnce()
        => Assert.Fail(Pending);

    // AC3 - Then the store no longer holds the second copy of U and C.
    // AC3 says "read for display or loaded for a run": T4 must cover both call sites (one spec per site)
    [Fact(Skip = Pending)]
    public void Read_RemovesTheRepeatedRowsFromTheStore()
        => Assert.Fail(Pending);

    // Scenario: sad path and edges

    // AC4 (merger level) - Given a chat already repaired; when its messages are read again, then no database write happens
    [Fact]
    public void SecondRead_OfARepairedChat_WritesNothing()
    {
        var rows = RepeatedRows();
        var repairedRows = ApplyRows(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        AIResentTurnMerger.RepairRepeatedTurns(repairedRows).IsEmpty.ShouldBeTrue();
    }

    // AC5 (merger level) - Given a chat with no repeated turn; when its messages are read, then no database write happens
    [Fact]
    public void Read_OfAChatWithNoRepeat_WritesNothing()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real")), User("u2"), Call("a2", "D"), Result("t2", ("D", "real")));

        AIResentTurnMerger.RepairRepeatedTurns(rows).IsEmpty.ShouldBeTrue();
    }

    // AC6 - Given the store fails partway through a repair; when the messages are read,
    // then the stored rows are unchanged and the read returns the unrepaired messages
    [Fact(Skip = Pending)]
    public void Read_WhenTheRepairFails_LeavesStoredRowsAndReturnsTheUnrepairedMessages()
        => Assert.Fail(Pending);
}
