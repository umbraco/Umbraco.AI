// MEM-09 — Other packages add their own memory spaces (AC1-AC6)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AC1, AC2: AIMemorySpaceResolverCollection (test resolver first, UserAgentMemorySpaceResolver last)
// through the provider. AC3, AC4: the memory Management API controllers (T14) over the real service.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class MemorySpaceResolverTests
{
    #region Happy path

    public class GivenAResolverThatClaimsRunsWithTestKey
    {
        public GivenAResolverThatClaimsRunsWithTestKey()
        {
            // Test resolver claims runs whose runtime context has property Test.Key -> space ("test", key).
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Run_LoadsFromTheResolversSpace()
        {
            // When: a run with Test.Key starts
            // Then: memoryBlock.ShouldContain(memoryInTestSpace.Text);
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Remember_SavesToTheResolversSpace()
        {
            // When: remember { layer: "space" } in that run
            // Then: repository.Stored.Single().SpaceType.ShouldBe("test");
        }
    }

    public class GivenAResolverThatClaimsNothing
    {
        public GivenAResolverThatClaimsNothing()
        {
            // Test resolver passes on every run.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void PrivateChat_FallsBackToUserAgent()
        {
            // When: a private chat starts
            // Then: resolvedSpace.ShouldBe(new AIMemorySpace("user-agent", userKey, agentId));
        }
    }

    #endregion

    #region Sad path

    public class GivenAResolverThatDeniesReadOnSpaceX
    {
        public GivenAResolverThatDeniesReadOnSpaceX()
        {
            // Test resolver owns space type X and returns false from its can-read check.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void GetMemories_ReturnsNotFound()
        {
            // When: GET memories?spaceType=X&spaceKey=...
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();  // or the API's 404 problem result
        }
    }

    public class GivenAResolverThatAllowsReadButDeniesWriteOnSpaceX
    {
        public GivenAResolverThatAllowsReadButDeniesWriteOnSpaceX()
        {
            // Test resolver: can-read true, can-write false; a memory in X.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void DeleteMemory_ReturnsNotFound()
        {
            // When: DELETE memories/{id}
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    public class GivenUserASAboutYouMemories
    {
        public GivenUserASAboutYouMemories()
        {
            // User A has "about you" memories; user B is signed in.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void GetMemoriesForAnotherUsersSpace_ReturnsNotFound()
        {
            // When: user B calls GET memories?spaceType=user&spaceKey=<A>
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    public class GivenNoResolverOwningSpaceTypeTeam
    {
        public GivenNoResolverOwningSpaceTypeTeam()
        {
            // No resolver in the collection owns space type "team".
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void GetMemoriesForTeam_ReturnsNotFound()
        {
            // When: a user calls GET memories?spaceType=team&spaceKey=<any>
            // Then: result.ShouldBeAssignableTo<NotFoundResult>();
        }
    }

    #endregion
}
