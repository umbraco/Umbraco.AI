// MEM-08 — Memory keeps the prompt cache warm (AC1-AC7)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// Real entry point: AIMemoryContextProvider (T11) invoked as AIAgentFactory attaches it, with the real
// AIMemoryService + AIMemoryTools over an in-memory IAIMemoryRepository fake. "The memory block" is the
// text resource the provider appends to Umbraco.AI.AdditionalResources; "the tools" are AIContext.Tools.
// Workspace turns reuse one AgentSession (state serialised between turns); the sidebar keeps none.
// "Trailing note" is what the provider appends to PendingSystemMessage.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class MemoryPromptCacheTests
{
    #region Happy path

    public class GivenAnAgentWithInstructionsAContextAndMemories
    {
        public GivenAnAgentWithInstructionsAContextAndMemories()
        {
            // Agent with instructions and an AI Context; user has memories; built through AIAgentFactory
            // and the system-message chat client so the final message list can be captured.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void MemoryBlock_SitsAfterTheContextAndBeforeHistory()
        {
            // When: a run starts
            // Then: indexOf(memoryBlock).ShouldBeInRange(indexOf(aiContext) + 1, indexOf(firstHistoryMessage) - 1);
        }
    }

    public class GivenAWorkspaceChatWithASaveDuringTurnOne
    {
        public GivenAWorkspaceChatWithASaveDuringTurnOne()
        {
            // Turn 1 runs and calls remember; turn 2 runs with the same session.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void TurnTwo_MemoryBlockIsByteIdenticalToTurnOne()
        {
            // When: turn 2's block is captured
            // Then: turnTwoBlock.ShouldBe(turnOneBlock);
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void TurnTwo_TrailingNoteHoldsTheNewMemory()
        {
            // When: turn 2's PendingSystemMessage is captured
            // Then: trailingNote.ShouldContain(newMemory.Text);  // under "changed since this chat started"
        }
    }

    public class GivenAWorkspaceChatWithAForgetDuringTurnOne
    {
        public GivenAWorkspaceChatWithAForgetDuringTurnOne()
        {
            // Memory A in the turn 1 block; turn 1 calls forget A; turn 2 runs.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void TurnTwo_TrailingNoteSaysItWasRemoved()
        {
            // When: turn 2's PendingSystemMessage is captured
            // Then: trailingNote.ShouldContain($"removed: {A.Text}");  // exact wording is the builder's
        }
    }

    public class GivenASidebarChatWithASaveDuringTurnOne
    {
        public GivenASidebarChatWithASaveDuringTurnOne()
        {
            // Sidebar run (no session state); turn 1 calls remember; turn 2 runs.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void TurnTwo_MemoryBlockHoldsTheNewMemory()
        {
            // When: turn 2's block is captured
            // Then: turnTwoBlock.ShouldContain(newMemory.Text);
        }
    }

    #endregion

    #region Sad path

    public class GivenAWorkspaceChatWithNoChangeSinceTurnOne
    {
        public GivenAWorkspaceChatWithNoChangeSinceTurnOne()
        {
            // Turn 1 neither saves nor forgets; turn 2 runs.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void TurnTwo_TrailingMessageHasNoMemoryNote()
        {
            // When: turn 2's PendingSystemMessage is captured
            // Then: (trailingNote ?? string.Empty).ShouldNotContain("changed since this chat started");
        }
    }

    public class GivenAnAgentWithMemoryOn
    {
        public GivenAnAgentWithMemoryOn()
        {
            // The Agent package's registered AITool collection (what the tool picker lists).
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void ToolPicker_DoesNotListRememberOrForget()
        {
            // When: the registered tools are listed
            // Then: toolIds.Intersect(new[] { "remember", "forget" }).ShouldBeEmpty();
        }
    }

    #endregion
}
