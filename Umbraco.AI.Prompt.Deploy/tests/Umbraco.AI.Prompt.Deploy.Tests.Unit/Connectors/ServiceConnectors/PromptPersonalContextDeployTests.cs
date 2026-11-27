// MEM-20 — Builders choose per prompt (AC10, AC11)
using Xunit;

namespace Umbraco.AI.Prompt.Deploy.Tests.Unit.Connectors.ServiceConnectors;

// Real entry point: UmbracoAIPromptServiceConnector, set up as in UmbracoAIPromptServiceConnectorTests
// (the IncludeEntityContext cases).
//
// Pending specs: each body is the intended arrange/act/assert as comments, so this file compiles
// before the types exist. The builder replaces the comments with code and removes the Skip.
public class PromptPersonalContextDeployTests
{
    #region Happy path

    public class GivenAPromptWithItOn
    {
        public GivenAPromptWithItOn()
        {
            // Prompt IncludePersonalContext = true.
        }

        // AC10
        [Fact(Skip = "Pending")]
        public void Artifact_HasTrue()
        {
            // When: the connector exports it
            // Then: artifact.IncludePersonalContext.ShouldBeTrue();
        }
    }

    public class GivenAnArtifactWithItOn
    {
        public GivenAnArtifactWithItOn()
        {
            // AIPromptArtifact IncludePersonalContext = true.
        }

        // AC11
        [Fact(Skip = "Pending")]
        public void SavedPrompt_HasTrue()
        {
            // When: the connector imports it
            // Then: savedPrompt.IncludePersonalContext.ShouldBeTrue();
        }
    }

    #endregion
}
