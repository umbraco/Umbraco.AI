// S1 - A frontend-tool turn is stored once (AC1-AC8)
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: a turn where the agent used a browser (frontend) tool is stored once, with the real tool result.
/// Pending until T2 (merger) and T3 (provider wiring) land.
/// </summary>
public class FrontendToolTurnStoredOnceTests
{
    private const string Pending = "Pending: T2/T3";

    // Scenario: the browser resends a turn the store already holds (happy path)
    // Given (AC1-AC6): the store holds user message U, tool call C and the stand-in result for C;
    // the browser resends U (same id), its copy of call C and the real result for C.
    // AC5 and AC6 start from their own stores, noted on each.

    // AC1 - Then U is not passed to the model a second time
    [Fact(Skip = Pending)]
    public void ResentTurn_WhenUserMessageResent_DropsItFromWhatTheModelGets()
        => Assert.Fail(Pending);

    // AC2 - Then the browser's assistant message with call C is not passed to the model
    [Fact(Skip = Pending)]
    public void ResentTurn_WhenToolCallResent_DropsTheBrowsersAssistantMessage()
        => Assert.Fail(Pending);

    // AC3 - Then the stored result for call C holds the real result, not "Success: Function completed."
    [Fact(Skip = Pending)]
    public void ResentTurn_WhenRealResultArrives_ReplacesTheStandInInTheStore()
        => Assert.Fail(Pending);

    // AC4 - Then the conversation stores U once, call C once and one result for C
    [Fact(Skip = Pending)]
    public void ResentTurn_WhenRunFinishes_StoresUserMessageCallAndResultOnce()
        => Assert.Fail(Pending);

    // AC5 - Given the store holds U, server call S with its real result, frontend call F and the stand-in for F;
    // the browser resends U, calls S and F, and the real results for S and F.
    // Then the stored result for S is unchanged and the stored result for F holds the real result
    [Fact(Skip = Pending)]
    public void ResentTurn_WithServerToolBeforeFrontendTool_LeavesServerResultUnchangedAndUpdatesFrontendResult()
        => Assert.Fail(Pending);

    // AC6 - When the resent turn is followed by a new user message N, then N is passed to the model and stored
    [Fact(Skip = Pending)]
    public void ResentTurn_FollowedByNewUserMessage_PassesAndStoresTheNewMessage()
        => Assert.Fail(Pending);

    // Scenario: sad path and edges

    // AC7 - Given the store holds a finished turn starting with user message U1 "Hello"; when a new run sends U2 "Hello" with a different id,
    // then U2 is passed to the model and stored as a new turn
    [Fact(Skip = Pending)]
    public void SameTextWithDifferentId_IsTreatedAsANewTurn()
        => Assert.Fail(Pending);

    // AC8 - Given a run paused for an approval; when the browser resumes it with an AG-UI resume entry,
    // then the run takes the existing approval path and the merge is not applied
    [Fact(Skip = Pending)]
    public void ApprovalResume_TakesTheExistingApprovalPathWithoutTheMerge()
        => Assert.Fail(Pending);
}
