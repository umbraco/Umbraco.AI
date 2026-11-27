// MEM-14 — Project memory (server side: AC1, AC2, AC8, AC9, AC11)
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Memory;

// Real entry point: WorkspaceMemorySpaceResolver registered ahead of the Agent package's resolvers, driven
// through the Agent package's AIMemoryContextProvider / memory API controllers, with the Workspace
// conversation, project and share services mocked at their interfaces.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class ProjectMemoryTests
{
    #region Happy path

    public class GivenAProjectWithAMemoryAndANewChatInIt
    {
        public GivenAProjectWithAMemoryAndANewChatInIt()
        {
            // Project P has a "project" memory; a new conversation in P.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockHoldsTheProjectMemory()
        {
            // When: the user sends a message
            // Then: memoryBlock.ShouldContain(projectMemory.Text);
        }
    }

    #endregion

    #region Sad path

    public class GivenAProjectWithAMemoryAndAPrivateChatOutsideIt
    {
        public GivenAProjectWithAMemoryAndAPrivateChatOutsideIt()
        {
            // Project P has a memory; the same user and agent chat outside any project.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockDoesNotHoldTheProjectMemory()
        {
            // When: the user sends a message
            // Then: memoryBlock.ShouldNotContain(projectMemory.Text);
        }
    }

    public class GivenAProjectOwnedByAnotherUser
    {
        public GivenAProjectOwnedByAnotherUser()
        {
            // Project owned by user 2; user 1 is signed in.
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void DeleteMemory_ReturnsNotFound()
        {
            // When: DELETE memories/{id} on one of its memories
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    public class GivenAProjectChatSharedWithAUserOutsideTheProject
    {
        public GivenAProjectChatSharedWithAUserOutsideTheProject()
        {
            // A conversation in project P shared with user 2, who isn't in P.
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void GetProjectMemories_ReturnsNotFound()
        {
            // When: user 2 calls GET memories?spaceType=project&spaceKey=P
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    public class GivenAProjectOwnerWithoutTheCopilotWorkspaceSection
    {
        public GivenAProjectOwnerWithoutTheCopilotWorkspaceSection()
        {
            // The owner of project P has lost the Copilot Workspace section.
        }

        // AC11
        [Fact(Skip = "Pending")]
        public void GetProjectMemories_ReturnsNotFound()
        {
            // When: the owner calls GET memories?spaceType=project&spaceKey=P
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    #endregion
}
