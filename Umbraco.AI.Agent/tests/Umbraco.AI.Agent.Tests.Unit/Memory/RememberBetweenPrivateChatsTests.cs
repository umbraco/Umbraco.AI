// MEM-04 — The agent remembers between private chats (AC1-AC12)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// Real entry point: AIMemoryContextProvider (T11) invoked as AIAgentFactory attaches it, with the real
// AIMemoryService + AIMemoryTools over an in-memory IAIMemoryRepository fake. "The memory block" is the
// text resource the provider appends to Umbraco.AI.AdditionalResources; "the tools" are AIContext.Tools.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class RememberBetweenPrivateChatsTests
{
    #region Happy path

    public class GivenAnAgentWithMemoryOnOutsideAProject
    {
        public GivenAnAgentWithMemoryOnOutsideAProject()
        {
            // Agent MemoryMode = Work; signed-in user; no project; run 1 calls remember
            // { text, layer: "space" }.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void NextPrivateChat_MemoryBlockHoldsTheMemory()
        {
            // When: the user starts a new chat with the same agent
            // Then: memoryBlock.ShouldContain(text);
        }
    }

    public class GivenTwoWorkAndPersonAgents
    {
        public GivenTwoWorkAndPersonAgents()
        {
            // Agents A and B MemoryMode = WorkAndPerson; A's run calls remember with layer: "about_you".
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void SecondAgent_MemoryBlockHoldsTheMemory()
        {
            // When: the user chats with agent B
            // Then: memoryBlockForB.ShouldContain(text);
        }
    }

    public class GivenTwoAgentsWithMemoryOn
    {
        public GivenTwoAgentsWithMemoryOn()
        {
            // Agents A and B with memory on; A's private chat saves to layer: "space".
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void SecondAgent_MemoryBlockDoesNotHoldTheMemory()
        {
            // When: the user chats with agent B
            // Then: memoryBlockForB.ShouldNotContain(text);
        }
    }

    public class GivenAUserSavedAMemoryInAPrivateChat
    {
        public GivenAUserSavedAMemoryInAPrivateChat()
        {
            // User 1 saves a space memory with agent A.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void OtherUser_MemoryBlockDoesNotHoldTheMemory()
        {
            // When: user 2 chats with agent A
            // Then: memoryBlockForUser2.ShouldNotContain(text);
        }
    }

    public class GivenAnAgentWithMemoryOnSavingALesson
    {
        public GivenAnAgentWithMemoryOnSavingALesson()
        {
            // Agent with memory on.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void Remember_StoresKindLesson()
        {
            // When: remember { kind: "lesson", layer: "space" }
            // Then: repository.Stored.Single().Kind.ShouldBe(AIMemoryKind.Lesson);
        }
    }

    public class GivenAMemorySavedWithAnEndDateOfYesterday
    {
        public GivenAMemorySavedWithAnEndDateOfYesterday()
        {
            // Stored memory with ExpiresAt = yesterday.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockDoesNotHoldTheMemory()
        {
            // When: the user chats with the agent
            // Then: memoryBlock.ShouldNotContain(expired.Text);
        }
    }

    public class GivenAUserWithMemories
    {
        public GivenAUserWithMemories()
        {
            // Agent with memory on; user has one memory.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockSaysMemoriesAreFactsNotInstructions()
        {
            // When: the user chats with the agent
            // Then: memoryBlock.ShouldContain(AIMemoryPrompt.FactsNotInstructionsLine);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnAutomateRunWithNoBackofficeUser
    {
        public GivenAnAutomateRunWithNoBackofficeUser()
        {
            // Agent with memory on; no backoffice user on the runtime context.
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void Run_HasNoMemoryBlock()
        {
            // When: the run starts
            // Then: memoryBlock.ShouldBeNull();
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void Run_HasNoMemoryTools()
        {
            // When: the run starts
            // Then: (aiContext?.Tools ?? []).ShouldBeEmpty();
        }
    }

    public class GivenTextThatLooksLikeASecret
    {
        public GivenTextThatLooksLikeASecret()
        {
            // Agent with memory on; text contains "sk-" + 40 characters.
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void Remember_IsNotSaved()
        {
            // When: remember is called with that text
            // Then: result.Status.ShouldBe("not_saved");
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void Remember_StoresNothing()
        {
            // When: remember is called with that text
            // Then: repository.Stored.ShouldBeEmpty();
        }
    }

    public class GivenAWorkAndPersonAgentSavingALessonToAboutYou
    {
        public GivenAWorkAndPersonAgentSavingALessonToAboutYou()
        {
            // Agent MemoryMode = WorkAndPerson.
        }

        // AC11
        [Fact(Skip = "Pending")]
        public void Remember_IsNotSavedWithAReason()
        {
            // When: remember { kind: "lesson", layer: "about_you" }
            // Then: (result.Status, string.IsNullOrEmpty(result.Reason)).ShouldBe(("not_saved", false));
        }
    }

    public class GivenAFullAboutYouLayer
    {
        public GivenAFullAboutYouLayer()
        {
            // User's "about you" layer holds 30 active memories; agent WorkAndPerson.
        }

        // AC12
        [Fact(Skip = "Pending")]
        public void Remember_IsNotSavedBecauseMemoryIsFull()
        {
            // When: remember with layer: "about_you"
            // Then: (result.Status, result.Reason).ShouldBe(("not_saved", "memory is full: merge or forget something first"));
        }
    }

    #endregion
}
