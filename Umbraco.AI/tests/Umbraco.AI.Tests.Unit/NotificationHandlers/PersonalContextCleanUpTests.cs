// MEM-21 — Personal context is cleaned up (AC1-AC3)
using Xunit;

namespace Umbraco.AI.Tests.Unit.NotificationHandlers;

// Real entry point: UserDeletedPersonalContextHandler and ContextDeletedPersonalContextHandler (T32)
// handling real notifications, over AIPersonalContextService and an in-memory repository fake.
// AC4 is in Umbraco.AI.Deploy.Tests.Unit (PersonalContextNotDeployedTests).
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class PersonalContextCleanUpTests
{
    #region Happy path

    public class GivenAUserWithAPersonalContext
    {
        public GivenAUserWithAPersonalContext()
        {
            // Anna has picked contexts and resources; Anna is deleted (UserDeletedNotification).
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void TheirSet_IsGone()
        {
            // When: the handler runs
            // Then: (await service.GetPersonalContextAsync(annaKey)).Resources.ShouldBeEmpty();
        }
    }

    public class GivenTwoUsersWhoPickedTheSameContext
    {
        public GivenTwoUsersWhoPickedTheSameContext()
        {
            // Anna and Sophie picked "Nordhem brand voice"; it's deleted (AIContextDeletedNotification).
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void NeitherList_ContainsIt()
        {
            // When: the handler runs
            // Then: annaSet.ContextIds.ShouldNotContain(id); sophieSet.ContextIds.ShouldNotContain(id);
        }
    }

    public class GivenTwoUsersWithResources
    {
        public GivenTwoUsersWithResources()
        {
            // Anna and Sophie have resources; Anna is deleted.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void OtherUsersResources_AreStillStored()
        {
            // When: the handler runs
            // Then: sophieSet.Resources.ShouldNotBeEmpty();
        }
    }

    #endregion
}
