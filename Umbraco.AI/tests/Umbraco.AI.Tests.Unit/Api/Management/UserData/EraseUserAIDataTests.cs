// MEM-24 — An admin erases a user's AI data (AC1, AC7-AC10)
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.UserData;

// Real entry point: the erase controller behind DELETE /umbraco/ai/management/api/v1/users/{userKey}/ai-data,
// over the real IAIUserDataEraseService (which publishes AIUserDataErasingNotification) and an in-memory
// IAIPersonalContextRepository fake, with a fake IUserService and IBackOfficeSecurityAccessor for the
// calling admin. AC2, AC3 are in the Agent test project (its notification handler), AC4-AC6, AC11, AC12
// are screen checks in the user-data frontend test.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class EraseUserAIDataTests
{
    #region Happy path

    public class GivenAUserWithPersonalContext
    {
        public GivenAUserWithPersonalContext()
        {
            // Sophie has picked contexts and resources; an admin with Users section access is signed in.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Erase_ReturnsNoContent()
        {
            // When: DELETE /users/<Sophie>/ai-data
            // Then: result.ShouldBeOfType<NoContentResult>();
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void Erase_EmptiesThePersonalContext()
        {
            // When: DELETE /users/<Sophie>/ai-data, then GET her personal context from the repository
            // Then: personalContext.Resources.ShouldBeEmpty();
        }
    }

    public class GivenAUserWithNoAIData
    {
        public GivenAUserWithNoAIData()
        {
            // Sophie has no personal context and no memories.
        }

        // AC7
        [Fact(Skip = "Pending")]
        public void Erase_ReturnsNoContent()
        {
            // When: DELETE /users/<Sophie>/ai-data
            // Then: result.ShouldBeOfType<NoContentResult>();
        }
    }

    #endregion

    #region Sad path

    public class GivenACallerWithoutUsersSectionAccess
    {
        public GivenACallerWithoutUsersSectionAccess()
        {
            // Signed in as an editor whose groups do not include the Users section.
        }

        // AC8
        [Fact(Skip = "Pending")]
        public void Erase_IsForbidden()
        {
            // When: DELETE /users/<Sophie>/ai-data through the test server
            // Then: response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    public class GivenAnAdminWhoCannotAccessThatUser
    {
        public GivenAnAdminWhoCannotAccessThatUser()
        {
            // Admin start nodes / group rules do not cover Sophie (IUserService access check denies).
        }

        // AC9
        [Fact(Skip = "Pending")]
        public void Erase_IsForbidden()
        {
            // When: DELETE /users/<Sophie>/ai-data
            // Then: response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    public class GivenNoUserWithThatKey
    {
        public GivenNoUserWithThatKey()
        {
            // IUserService returns null for the key.
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void Erase_ReturnsNotFound()
        {
            // When: DELETE /users/<unknownKey>/ai-data
            // Then: response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    #endregion
}
