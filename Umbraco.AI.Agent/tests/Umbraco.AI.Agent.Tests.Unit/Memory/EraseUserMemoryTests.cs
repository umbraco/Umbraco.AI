// MEM-24 — An admin erases a user's AI data (AC2, AC3)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// Real entry point: UserDataErasingMemoryHandler handling AIUserDataErasingNotification (published by
// core's erase endpoint) over the real AIMemoryService and an in-memory IAIMemoryRepository fake.
// The endpoint, dialog and toast (AC1, AC4-AC12) belong to core and its own specs.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class EraseUserMemoryTests
{
    #region Happy path

    public class GivenSophieWithMemoriesOfEveryKind
    {
        public GivenSophieWithMemoriesOfEveryKind()
        {
            // Sophie has "about you", "you + this agent", held and forgotten memories.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void Erase_LeavesNoneOfHerOwnRows()
        {
            // When: the AIUserDataErasingNotification for Sophie is handled by UserDataErasingMemoryHandler
            // Then: repository.Stored.Where(m => m.SpaceKey == sophieKey).ShouldBeEmpty();
        }
    }

    #endregion

    #region Sad path

    public class GivenSophieSavedAMemoryInAProject
    {
        public GivenSophieSavedAMemoryInAProject()
        {
            // Sophie saved a memory into a project space (credited to her).
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Erase_LeavesTheProjectMemory()
        {
            // When: the AIUserDataErasingNotification for Sophie is handled by UserDataErasingMemoryHandler
            // Then: repository.Find(projectMemory.Id).ShouldNotBeNull();
        }
    }

    #endregion
}
