// MEM-10 — Hooks around saving and deleting (AC1-AC5)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AIMemoryService with a mocked IEventAggregator (as AIConversationServiceNotificationTests does);
// AC1, AC2 via the remember tool, AC4 via the DELETE controller.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class MemoryNotificationsTests
{
    #region Happy path

    public class GivenAHandlerForSaved
    {
        public GivenAHandlerForSaved()
        {
            // IEventAggregator captures AIMemorySavedNotification.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Save_PublishesTheSavedMemory()
        {
            // When: a memory is saved
            // Then: savedNotification!.Memory.Text.ShouldBe(text);
        }
    }

    public class GivenAHandlerForDeleted
    {
        public GivenAHandlerForDeleted()
        {
            // IEventAggregator captures AIMemoryDeletedNotification.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void Delete_PublishesTheDeletedMemory()
        {
            // When: a memory is deleted
            // Then: deletedNotification!.Memory.Id.ShouldBe(memory.Id);
        }
    }

    #endregion

    #region Sad path

    public class GivenAHandlerThatCancelsSaving
    {
        public GivenAHandlerThatCancelsSaving()
        {
            // IEventAggregator cancels AIMemorySavingNotification.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Remember_IsNotSaved()
        {
            // When: the agent calls remember
            // Then: result.Status.ShouldBe("not_saved");
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void Remember_StoresNothing()
        {
            // When: the agent calls remember
            // Then: repository.Stored.ShouldBeEmpty();
        }
    }

    public class GivenAHandlerThatCancelsDeleting
    {
        public GivenAHandlerThatCancelsDeleting()
        {
            // IEventAggregator cancels AIMemoryDeletingNotification; a stored memory.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void Delete_KeepsTheMemoryStored()
        {
            // When: DELETE memories/{id} through the API
            // Then: repository.Find(memory.Id).ShouldNotBeNull();
        }
    }

    #endregion
}
