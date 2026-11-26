// MEM-07 — Forget in chat (AC1-AC7)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// Real entry point: AIMemoryContextProvider (T11) invoked as AIAgentFactory attaches it, with the real
// AIMemoryService + AIMemoryTools over an in-memory IAIMemoryRepository fake. "The memory block" is the
// text resource the provider appends to Umbraco.AI.AdditionalResources; "the tools" are AIContext.Tools.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class ForgetInChatTests
{
    #region Happy path

    public class GivenAMemoryInTheBlock
    {
        public GivenAMemoryInTheBlock()
        {
            // Memory A injected into the run's block.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Forget_ReturnsForgotten()
        {
            // When: forget { id: A short id }
            // Then: result.Status.ShouldBe("forgotten");
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void Forget_RemovesItFromStorage()
        {
            // When: forget { id: A short id }
            // Then: repository.Find(A.Id).ShouldBeNull();
        }
    }

    #endregion

    #region Sad path

    public class GivenAnIdNotInTheBlock
    {
        public GivenAnIdNotInTheBlock()
        {
            // A short id matching nothing the run can see.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Forget_ReturnsNotFound()
        {
            // When: forget { id }
            // Then: result.Status.ShouldBe("not_found");
        }
    }

    public class GivenAShortIdThatMatchesAnotherUsersMemory
    {
        public GivenAShortIdThatMatchesAnotherUsersMemory()
        {
            // User 2 owns a memory whose short id is passed by user 1's run.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void Forget_ReturnsNotFound()
        {
            // When: forget { id }
            // Then: result.Status.ShouldBe("not_found");
        }
    }

    public class GivenTwoMemoriesWhoseIdsShareTheirFirst8Characters
    {
        public GivenTwoMemoriesWhoseIdsShareTheirFirst8Characters()
        {
            // Two of the run's memories with Guids that share their first 8 hex characters.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void TheirShortIds_Differ()
        {
            // When: the memory block is built (AIShortIdMap over the run's memories)
            // Then: shortIdA.ShouldNotBe(shortIdB);
        }
    }

    public class GivenAShortIdThatMatchesTwoOfTheRunsMemories
    {
        public GivenAShortIdThatMatchesTwoOfTheRunsMemories()
        {
            // An 8-character id that is a prefix of two memories this run can see.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void Forget_ReturnsNotFound()
        {
            // When: forget { id }
            // Then: result.Status.ShouldBe("not_found");
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void Forget_LeavesBothMemoriesStored()
        {
            // When: forget { id }
            // Then: repository.Count().ShouldBe(2);
        }
    }

    #endregion
}
