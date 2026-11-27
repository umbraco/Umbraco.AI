// MEM-02 — Site-wide memory settings (AC1-AC6; AC7, AC8 are screen-only)
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Memory;

// AC1, AC2: the server half (the registered defaults the AI Settings panel reads); the toggle and
// dropdown themselves are checked by T17's live acceptance. AC3-AC6 go through the provider:
// Real entry point: AIMemoryContextProvider (T11) invoked as AIAgentFactory attaches it, with the real
// AIMemoryService + AIMemoryTools over an in-memory IAIMemoryRepository fake. "The memory block" is the
// text resource the provider appends to Umbraco.AI.AdditionalResources; "the tools" are AIContext.Tools.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class SiteWideMemorySettingsTests
{
    #region Happy path

    public class GivenAFreshInstallWithTheAgentPackage
    {
        public GivenAFreshInstallWithTheAgentPackage()
        {
            // AIMemorySettings registered into AISettingDefinitions(); nothing saved.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void MemoryEnabled_DefaultsToTrue()
        {
            // When: IAISettingsService.GetSettingValueAsync<bool>("Agent.Memory.Enabled")
            // Then: value.ShouldBeTrue();
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void PersonalMemory_DefaultsToAllowed()
        {
            // When: IAISettingsService.GetSettingValueAsync<AIPersonalMemoryPolicy>("Agent.Memory.PersonalMemory")
            // Then: value.ShouldBe(AIPersonalMemoryPolicy.Allowed);
        }
    }

    public class GivenTheSwitchWasOffAndIsSavedOnAgain
    {
        public GivenTheSwitchWasOffAndIsSavedOnAgain()
        {
            // User has saved memories; Agent.Memory.Enabled saved false, then saved true.
            // Agent set to WorkAndPerson.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockHoldsTheSavedMemories()
        {
            // When: the user chats again
            // Then: memoryBlock.ShouldContain(savedMemory.Text);
        }
    }

    public class GivenPersonalMemoryIsNotAllowed
    {
        public GivenPersonalMemoryIsNotAllowed()
        {
            // User has "about you" memories; Agent.Memory.PersonalMemory saved NotAllowed.
        }

        // AC6
        [Fact(Skip = "Pending")]
        public void AboutYouMemories_AreStillStored()
        {
            // When: the repository is read for the user's "user" space
            // Then: stored.Count.ShouldBe(aboutYouCount);
        }
    }

    #endregion

    #region Sad path

    public class GivenTheSiteSwitchIsOffAndTheAgentIsWorkAndPerson
    {
        public GivenTheSiteSwitchIsOffAndTheAgentIsWorkAndPerson()
        {
            // Agent.Memory.Enabled saved false; agent MemoryMode = WorkAndPerson; user has memories.
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Run_GetsNoMemoryBlock()
        {
            // When: the user chats with the agent
            // Then: memoryBlock.ShouldBeNull();
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void Run_GetsNoMemoryTools()
        {
            // When: the user chats with the agent
            // Then: (aiContext?.Tools ?? []).ShouldBeEmpty();  // no remember / forget
        }
    }

    public class GivenPersonalMemoryIsNotAllowedAndTheAgentIsWorkAndPerson
    {
        public GivenPersonalMemoryIsNotAllowedAndTheAgentIsWorkAndPerson()
        {
            // Agent MemoryMode = WorkAndPerson; user has "about you" memories;
            // Agent.Memory.PersonalMemory saved NotAllowed.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void Run_MemoryBlockHasNoAboutYouMemories()
        {
            // When: the user chats with the agent
            // Then: memoryBlock.ShouldNotContain(aboutYouMemory.Text);
        }
    }

    #endregion
}
