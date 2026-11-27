// MEM-21 — Personal context is cleaned up (AC4)
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors;

// Real entry point: the AI Context service connector's export, with a context service fake that holds
// one AI Context and a personal context service fake that holds a user's resources.
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class PersonalContextNotDeployedTests
{
    #region Happy path

    public class GivenAUserWithPersonalResources
    {
        public GivenAUserWithPersonalResources()
        {
            // Anna has personal resource "How I write product pages"; one AI Context exists.
        }

        // AC4
        [Fact(Skip = "Pending")]
        public void ContextExport_HasNoPersonalResources()
        {
            // When: Deploy exports AI Contexts
            // Then: artifacts.SelectMany(a => a.Resources).ShouldNotContain(r => r.Name == "How I write product pages");
        }
    }

    #endregion
}
