using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI guardrail.
/// </summary>
public class AIGuardrailArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The guardrail rules serialized as JSON.
    /// </summary>
    public JsonElement? Rules { get; set; }
}
