// MEM-20 — Builders choose per prompt (AC5-AC7, AC9); MEM-19 — prompts use it (AC6, AC9)
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

// Real entry point: AIPromptService (create, update, run) over the persistence used by
// AllowedUserGroupIdsPersistenceTests, plus PromptMapDefinition for the API shape. AC6 (upgrade)
// uses the migration on a SQLite database seeded with a prompt row. AC8 (the toggle) is checked by
// T40's live acceptance: the Prompt client has no component tests.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class PromptPersonalContextTests
{
    #region Happy path

    public class GivenANewPrompt
    {
        public GivenANewPrompt()
        {
            // Create a prompt without setting UsePersonalContext.
        }

        // MEM-20 AC5
        [Fact(Skip = "Pending")]
        public void UsePersonalContext_IsFalse()
        {
            // When: it's saved and read
            // Then: prompt.UsePersonalContext.ShouldBeFalse();
        }
    }

    public class GivenAPromptSavedBeforeThisRelease
    {
        public GivenAPromptSavedBeforeThisRelease()
        {
            // SQLite database with a prompt row from before UmbracoAIPrompt_AddUsePersonalContext.
        }

        // MEM-20 AC6
        [Fact(Skip = "Pending")]
        public void AfterMigration_IsFalse()
        {
            // When: the migration runs and the prompt is read
            // Then: prompt.UsePersonalContext.ShouldBeFalse();
        }
    }

    public class GivenAnAdminWhoTurnsItOn
    {
        public GivenAnAdminWhoTurnsItOn()
        {
            // Update request with UsePersonalContext = true.
        }

        // MEM-20 AC7
        [Fact(Skip = "Pending")]
        public void Get_ReturnsTrue()
        {
            // When: GET the prompt
            // Then: response.UsePersonalContext.ShouldBeTrue();
        }

        // MEM-20 AC9
        [Fact(Skip = "Pending")]
        public void NewVersion_RecordsTrue()
        {
            // When: the version history is read
            // Then: latestVersion snapshot has UsePersonalContext true
        }
    }

    public class GivenAPromptWithItOn
    {
        public GivenAPromptWithItOn()
        {
            // Prompt UsePersonalContext = true; run it.
        }

        // MEM-19 AC6
        [Fact(Skip = "Pending")]
        public void Run_SetsTheFlag()
        {
            // When: the prompt runs
            // Then: runtimeContext.GetValue<bool>(ContextKeys.UsePersonalContext).ShouldBeTrue();
        }
    }

    #endregion

    #region Sad path

    public class GivenAPromptWithItOff
    {
        public GivenAPromptWithItOff()
        {
            // Prompt UsePersonalContext = false; run it.
        }

        // MEM-19 AC9
        [Fact(Skip = "Pending")]
        public void Run_DoesNotSetTheFlag()
        {
            // When: the prompt runs
            // Then: runtimeContext.GetValue<bool?>(ContextKeys.UsePersonalContext).ShouldNotBe(true);
        }
    }

    #endregion
}
