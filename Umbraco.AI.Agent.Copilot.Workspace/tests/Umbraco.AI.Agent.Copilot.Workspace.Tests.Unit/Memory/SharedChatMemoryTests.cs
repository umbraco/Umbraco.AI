// MEM-15 — Shared chat memory (server side: AC1-AC3, AC6, AC7)
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Memory;

// Real entry point: WorkspaceMemorySpaceResolver registered ahead of the Agent package's resolvers, driven
// through the Agent package's AIMemoryContextProvider / memory API controllers, with the Workspace
// conversation, project and share services mocked at their interfaces.
// AC1, AC2 also cover IAIConversationShareService.ConversationHasEverBeenSharedAsync (T21).
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class SharedChatMemoryTests
{
    #region Happy path

    public class GivenAPrivateChatThatHasJustBeenShared
    {
        public GivenAPrivateChatThatHasJustBeenShared()
        {
            // Conversation C outside a project, with one active share.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void RememberToSpace_StoresInTheConversationSpace()
        {
            // When: remember { layer: "space" }
            // Then: (stored.SpaceType, stored.SpaceKey).ShouldBe(("conversation", C));
        }
    }

    public class GivenAChatWhoseOnlyShareWasRevoked
    {
        public GivenAChatWhoseOnlyShareWasRevoked()
        {
            // Conversation C's only share row has DateRevoked set.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void RememberToSpace_StillStoresInTheConversationSpace()
        {
            // When: remember { layer: "space" }
            // Then: (stored.SpaceType, stored.SpaceKey).ShouldBe(("conversation", C));
        }
    }

    public class GivenASharedChatWithMemoriesReadByARecipient
    {
        public GivenASharedChatWithMemoriesReadByARecipient()
        {
            // Conversation C shared with user 2; C's space has memories.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void GetConversationMemories_ReturnsThem()
        {
            // When: user 2 calls GET memories?spaceType=conversation&spaceKey=C
            // Then: ((IEnumerable<MemoryResponseModel>)((OkObjectResult)result).Value!).Count().ShouldBe(memoryCount);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnOwnerWithPrivateMemoriesAndASharedChat
    {
        public GivenAnOwnerWithPrivateMemoriesAndASharedChat()
        {
            // Owner has user-agent memories with the agent; conversation C was shared.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockDoesNotHoldPrivateMemories()
        {
            // When: the owner sends a message in C
            // Then: memoryBlock.ShouldNotContain(privateMemory.Text);
        }
    }

    public class GivenASharedChatWithAMemoryAndARecipient
    {
        public GivenASharedChatWithAMemoryAndARecipient()
        {
            // Conversation C shared with user 2; one memory in C's space.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void RecipientDelete_ReturnsNotFound()
        {
            // When: user 2 calls DELETE memories/{id}
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    #endregion
}
