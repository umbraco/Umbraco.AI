// MEM-13 — Edit a memory (server side of AC1, AC2, AC6)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// Real entry point: PUT memories/{id} (T14) over AIMemoryService. The dialog itself (counter, disabled
// Save, error under the field, dialog stays open) is checked by T18's live acceptance: the Agent
// backoffice has no test harness.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class EditAMemoryTests
{
    #region Happy path

    public class GivenAMemoryWhoseTextIsChanged
    {
        public GivenAMemoryWhoseTextIsChanged()
        {
            // User owns a memory; PUT memories/{id} { text: "new text" }.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void List_ShowsTheNewText()
        {
            // When: GET memories/mine
            // Then: response.AboutYou.Single().Text.ShouldBe("new text");
        }
    }

    public class GivenAMemoryWithNoEndDateGivenOne
    {
        public GivenAMemoryWithNoEndDateGivenOne()
        {
            // PUT memories/{id} { text, expiresAt: 2027-03-01 }.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void List_ShowsTheEndDate()
        {
            // When: GET memories/mine
            // Then: response.AboutYou.Single().ExpiresAt!.Value.Date.ShouldBe(new DateTime(2027, 3, 1));
        }
    }

    #endregion

    #region Sad path

    public class GivenTextThatLooksLikeAnApiKey
    {
        public GivenTextThatLooksLikeAnApiKey()
        {
            // PUT memories/{id} { text: "sk-" + 40 characters }.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void Update_ReturnsBadRequest()
        {
            // When: the request is handled
            // Then: result.ShouldBeAssignableTo<BadRequestObjectResult>();
        }
    }

    #endregion
}
