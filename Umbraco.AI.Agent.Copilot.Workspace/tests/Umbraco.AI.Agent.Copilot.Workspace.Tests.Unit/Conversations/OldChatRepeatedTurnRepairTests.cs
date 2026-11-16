// S2 - An old chat with a repeated turn repairs itself (AC1-AC6)
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: an old chat that already holds a repeated turn repairs itself when read.
/// Pending until T2 (merger) and T4 (repair on read) land.
/// </summary>
public class OldChatRepeatedTurnRepairTests
{
    private const string Pending = "Pending: T2/T4";

    // Scenario: a chat holding a repeated turn (happy path)
    // Given (AC1-AC3): the store holds U, call C, the stand-in for C, then U again, call C again and the real result for C.

    // AC1 - Then the returned messages hold U once, call C once and the real result for C
    [Fact(Skip = Pending)]
    public void ReadForDisplay_ReturnsTheRepeatedTurnOnce()
        => Assert.Fail(Pending);

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

    // AC4 - Given a chat already repaired; when its messages are read again, then no database write happens
    [Fact(Skip = Pending)]
    public void SecondRead_OfARepairedChat_WritesNothing()
        => Assert.Fail(Pending);

    // AC5 - Given a chat with no repeated turn; when its messages are read, then no database write happens
    [Fact(Skip = Pending)]
    public void Read_OfAChatWithNoRepeat_WritesNothing()
        => Assert.Fail(Pending);

    // AC6 - Given the store fails partway through a repair; when the messages are read,
    // then the stored rows are unchanged and the read returns the unrepaired messages
    [Fact(Skip = Pending)]
    public void Read_WhenTheRepairFails_LeavesStoredRowsAndReturnsTheUnrepairedMessages()
        => Assert.Fail(Pending);
}
