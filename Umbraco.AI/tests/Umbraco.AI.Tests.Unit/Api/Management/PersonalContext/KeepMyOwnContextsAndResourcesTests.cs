// MEM-18 — Keep my own contexts and resources (AC1, AC3-AC6, AC9, AC10)
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.PersonalContext;

// Real entry point: the personal context Management API controllers (T33) over the real
// AIPersonalContextService (T30) and an in-memory IAIPersonalContextRepository fake, signed in as a
// fake backoffice user. AC2, AC7, AC8, AC11-AC13 are screen checks in
// your-ai-user-profile-app.element.test.ts.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class KeepMyOwnContextsAndResourcesTests
{
    #region Happy path

    public class GivenAUserWhoPicksAContext
    {
        public GivenAUserWhoPicksAContext()
        {
            // Signed in as Anna; PUT /personal-context { contextIds: [nordhemBrandVoiceId], resources: [] }.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Get_ReturnsTheContextId()
        {
            // When: GET /personal-context
            // Then: response.ContextIds.ShouldContain(nordhemBrandVoiceId);
        }
    }

    public class GivenAUserWhoAddsAnAlwaysTextResource
    {
        public GivenAUserWhoAddsAnAlwaysTextResource()
        {
            // PUT a Text resource "How I write product pages" with InjectionMode Always.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Get_ReturnsItAsAlways()
        {
            // When: GET /personal-context
            // Then: response.Resources.Single().InjectionMode.ShouldBe("Always");
        }
    }

    public class GivenAUserWhoRemovesAResource
    {
        public GivenAUserWhoRemovesAResource()
        {
            // Saved set holds "Spring catalogue 2027"; PUT the set without it.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void Get_NoLongerReturnsIt()
        {
            // When: GET /personal-context
            // Then: response.Resources.ShouldNotContain(r => r.Name == "Spring catalogue 2027");
        }
    }

    public class GivenTwoUsersWithTheirOwnSets
    {
        public GivenTwoUsersWithTheirOwnSets()
        {
            // Anna and Sophie each PUT a different set.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void Get_ReturnsOnlyTheCallersSet()
        {
            // When: Anna calls GET /personal-context
            // Then: response.Resources.ShouldAllBe(r => annaResourceIds.Contains(r.Id));
        }
    }

    public class GivenAUserWhoNeverSavedOne
    {
        public GivenAUserWhoNeverSavedOne()
        {
            // No stored set for the signed-in user.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void Get_ReturnsEmptyLists()
        {
            // When: GET /personal-context
            // Then: status 200; response.ContextIds.ShouldBeEmpty(); response.Resources.ShouldBeEmpty();
        }
    }

    #endregion

    #region Sad path

    public class GivenAContextTheUserCannotList
    {
        public GivenAContextTheUserCannotList()
        {
            // The context service's list for this user doesn't include restrictedContextId.
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void Put_Returns400()
        {
            // When: PUT /personal-context { contextIds: [restrictedContextId] }
            // Then: result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    public class GivenAResourceWithAnUnknownType
    {
        public GivenAResourceWithAnUnknownType()
        {
            // Resource with ResourceTypeId "not-a-type".
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void Put_Returns400()
        {
            // When: PUT /personal-context with that resource
            // Then: result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    #endregion
}
