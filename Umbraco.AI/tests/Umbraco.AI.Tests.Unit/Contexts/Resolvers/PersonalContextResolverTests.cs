// MEM-19 — Agents and prompts use my personal context (AC1-AC5, AC10, AC11)
using Xunit;

namespace Umbraco.AI.Tests.Unit.Contexts.Resolvers;

// Real entry point: IAIContextResolutionService with the real resolver chain (Profile, Content,
// KnowledgeSet, PersonalContextResolver, AdditionalResources), the runtime context accessor holding
// ContextKeys.UsePersonalContext = true unless the scenario says otherwise, and a fake backoffice
// user. "The system prompt" is the formatted Always block; "the lookup tool" is the OnDemand tool list.
// AC6-AC9 are in the Agent and Prompt test projects (they set the flag).
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class PersonalContextResolverTests
{
    #region Happy path

    public class GivenAnAlwaysResourceAndTheFlagSet
    {
        public GivenAnAlwaysResourceAndTheFlagSet()
        {
            // Anna has Text resource "How I write product pages", Always.
        }

        // AC1
        [Fact(Skip = "Pending")]
        public void SystemPrompt_ContainsTheResource()
        {
            // When: context is resolved and formatted
            // Then: systemPrompt.ShouldContain("How I write product pages");
        }
    }

    public class GivenAnOnDemandResourceAndTheFlagSet
    {
        public GivenAnOnDemandResourceAndTheFlagSet()
        {
            // Anna has Text resource "Spring catalogue 2027", OnDemand.
        }

        // AC2
        [Fact(Skip = "Pending")]
        public void LookupTool_ListsTheResource()
        {
            // When: context is resolved
            // Then: onDemandResources.ShouldContain(r => r.Name == "Spring catalogue 2027");
        }

        // AC3
        [Fact(Skip = "Pending")]
        public void SystemPrompt_DoesNotContainTheResource()
        {
            // When: context is resolved and formatted
            // Then: systemPrompt.ShouldNotContain("Spring catalogue 2027");
        }
    }

    public class GivenAPickedContextAndTheFlagSet
    {
        public GivenAPickedContextAndTheFlagSet()
        {
            // Anna picked "Nordhem brand voice", which has an Always resource "Tone rules".
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void SystemPrompt_ContainsTheContextsResource()
        {
            // When: context is resolved and formatted
            // Then: systemPrompt.ShouldContain("Tone rules");
        }
    }

    public class GivenTheSameContextFromTheAgentAndTheUser
    {
        public GivenTheSameContextFromTheAgentAndTheUser()
        {
            // AgentId set to an agent with "Nordhem brand voice"; Anna picked it too.
        }

        // AC5
        [Fact(Skip = "Pending")]
        public void ContextsResources_AppearOnce()
        {
            // When: context is resolved
            // Then: resolved.Resources.Count(r => r.Name == "Tone rules").ShouldBe(1);
        }
    }

    #endregion

    #region Sad path

    public class GivenNoBackofficeUser
    {
        public GivenNoBackofficeUser()
        {
            // Flag set; no signed-in backoffice user (an Automate run).
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void Resolver_ReturnsNothing()
        {
            // When: PersonalContextResolver.ResolveAsync()
            // Then: result.Resources.ShouldBeEmpty();
        }
    }

    public class GivenASignedInUserButNoFlag
    {
        public GivenASignedInUserButNoFlag()
        {
            // Anna has an Always resource; ContextKeys.UsePersonalContext not set.
        }

        // AC11
        [Fact(Skip = "Pending")]
        public void Resolver_ReturnsNothing()
        {
            // When: PersonalContextResolver.ResolveAsync()
            // Then: result.Resources.ShouldBeEmpty();
        }
    }

    #endregion
}
