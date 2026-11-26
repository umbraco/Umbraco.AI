// MEM-16 — Memory is cleaned up with what it belongs to (AC1-AC3, AC6, AC7)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AC1-AC3: UserDeletedMemoryHandler / AgentDeletedMemoryHandler handling the CMS user-deleted and
// AIAgentDeletedNotification. AC6, AC7: AIMemoryService list reads over the repository fake.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class MemoryCleanUpTests
{
    #region Happy path

    public class GivenAUserWithOwnLayers
    {
        public GivenAUserWithOwnLayers()
        {
            // User has "user" and "user-agent" memories; the user-deleted notification is handled.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void UserDeleted_RemovesTheirOwnLayers()
        {
            // When: the repository is read for that user
            // Then: repository.Stored.Where(m => m.SpaceKey == userKey).ShouldBeEmpty();
        }
    }

    public class GivenAUserWhoSavedIntoAProject
    {
        public GivenAUserWhoSavedIntoAProject()
        {
            // User saved a "project" memory; the user-deleted notification is handled.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void UserDeleted_KeepsTheProjectMemory()
        {
            // When: the repository is read
            // Then: repository.Find(projectMemory.Id).ShouldNotBeNull();
        }
    }

    public class GivenUserAgentMemoriesForAgentA
    {
        public GivenUserAgentMemoriesForAgentA()
        {
            // "user-agent" memories for agent A; AIAgentDeletedNotification for A is handled.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void AgentDeleted_RemovesItsPrivateMemories()
        {
            // When: the repository is read
            // Then: repository.Stored.Where(m => m.AgentId == agentA).ShouldBeEmpty();
        }
    }

    public class GivenAMemoryWhoseEndDatePassed
    {
        public GivenAMemoryWhoseEndDatePassed()
        {
            // A stored memory with ExpiresAt in the past.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void ListRead_DeletesTheRow()
        {
            // When: its list is read
            // Then: repository.Find(expired.Id).ShouldBeNull();
        }
    }

    public class GivenAMemoryReplaced8DaysAgo
    {
        public GivenAMemoryReplaced8DaysAgo()
        {
            // A memory with Status Replaced, DateModified = now - 8 days.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void ListRead_DeletesTheReplacedRow()
        {
            // When: its layer's list is read
            // Then: repository.Find(replaced.Id).ShouldBeNull();
        }
    }

    #endregion
}
