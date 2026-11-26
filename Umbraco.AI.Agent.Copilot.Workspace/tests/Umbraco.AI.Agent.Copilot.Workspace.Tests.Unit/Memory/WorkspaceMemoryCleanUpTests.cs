// MEM-16 — Memory is cleaned up with what it belongs to (AC4, AC5)
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Memory;

// ProjectDeletedMemoryHandler / ConversationDeletedMemoryHandler handling AIProjectDeletedNotification
// and AIConversationDeletedNotification, with IAIMemoryService mocked or over a repository fake.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class WorkspaceMemoryCleanUpTests
{
    #region Happy path

    public class GivenAProjectWithMemories
    {
        public GivenAProjectWithMemories()
        {
            // Project P has memories; AIProjectDeletedNotification for P is handled.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void ProjectDeleted_RemovesItsMemories()
        {
            // When: P's space is read
            // Then: memoriesIn("project", P).ShouldBeEmpty();
        }
    }

    public class GivenASharedChatWithMemories
    {
        public GivenASharedChatWithMemories()
        {
            // Conversation C has memories; AIConversationDeletedNotification for C is handled.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void ConversationDeleted_RemovesItsMemories()
        {
            // When: C's space is read
            // Then: memoriesIn("conversation", C).ShouldBeEmpty();
        }
    }

    #endregion
}
