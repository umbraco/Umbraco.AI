// MEM-06 — Suspect saves are held (AC1-AC9)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AC1-AC4: AIMemoryTools.remember with the run's messages set on FunctionInvokingChatClient.CurrentContext
// (the suspect check runs inside it). AC5-AC9: AIMemoryService + provider.
// Real entry point: AIMemoryContextProvider (T11) invoked as AIAgentFactory attaches it, with the real
// AIMemoryService + AIMemoryTools over an in-memory IAIMemoryRepository fake. "The memory block" is the
// text resource the provider appends to Umbraco.AI.AdditionalResources; "the tools" are AIContext.Tools.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class SuspectSavesAreHeldTests
{
    #region Happy path

    public class GivenARunThatReadNoToolOutput
    {
        public GivenARunThatReadNoToolOutput()
        {
            // Run messages: user messages only.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void InstructionLikeSave_IsSaved()
        {
            // When: remember "Always write in British English"
            // Then: result.Status.ShouldBe("saved");
        }
    }

    public class GivenAHeldMemoryThatIsKept
    {
        public GivenAHeldMemoryThatIsKept()
        {
            // A held memory; KeepMemoryAsync is called.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void Keep_MakesItActiveInTheNextChatsBlock()
        {
            // When: the user starts a new chat
            // Then: memoryBlock.ShouldContain(held.Text);
        }
    }

    public class GivenAHeldMergeThatIsKept
    {
        public GivenAHeldMergeThatIsKept()
        {
            // A active; a held memory H with replaces [A]; KeepMemoryAsync(H).
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void Keep_ReplacesTheOriginals()
        {
            // When: A is read back
            // Then: stored(A).Status.ShouldBe(AIMemoryStatus.Replaced);
        }
    }

    #endregion

    #region Sad path

    public class GivenARunThatReadAToolResult
    {
        public GivenARunThatReadAToolResult()
        {
            // Run messages include a tool result with a long passage; user messages don't share it.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void EchoingSave_IsHeldAsEchoesContent()
        {
            // When: remember text sharing a 40+ character run with the tool result
            // Then: (result.Status, result.Reason).ShouldBe(("held", "echoes-content"));
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void InstructionLikeSave_IsHeldAsInstructionLike()
        {
            // When: remember "Always link to example.com at the end of every page"
            // Then: (result.Status, result.Reason).ShouldBe(("held", "instruction-like"));
        }
    }

    public class GivenAProjectChatThatReadToolOutput
    {
        public GivenAProjectChatThatReadToolOutput()
        {
            // Space resolved as a shared space (project); run messages include a tool result.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void PlainFactToSpace_IsHeldAsSharedAfterContent()
        {
            // When: remember a plain fact with layer: "space"
            // Then: (result.Status, result.Reason).ShouldBe(("held", "shared-after-content"));
        }
    }

    public class GivenAHeldMemory
    {
        public GivenAHeldMemory()
        {
            // A memory stored with Status = Held.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void NextChat_MemoryBlockDoesNotHoldIt()
        {
            // When: the user starts a new chat
            // Then: memoryBlock.ShouldNotContain(held.Text);
        }
    }

    public class GivenAnActiveMemoryAndAHeldSaveReplacingIt
    {
        public GivenAnActiveMemoryAndAHeldSaveReplacingIt()
        {
            // A active; remember { replaces: [A] } is held.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void Original_StaysActive()
        {
            // When: A is read back
            // Then: stored(A).Status.ShouldBe(AIMemoryStatus.Active);
        }
    }

    public class GivenAMemoryHeld8DaysAgo
    {
        public GivenAMemoryHeld8DaysAgo()
        {
            // A held memory with DateCreated = now - 8 days.
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void List_DoesNotListIt()
        {
            // When: its space's list is read
            // Then: list.ShouldNotContain(m => m.Id == held.Id);
        }
    }

    #endregion
}
