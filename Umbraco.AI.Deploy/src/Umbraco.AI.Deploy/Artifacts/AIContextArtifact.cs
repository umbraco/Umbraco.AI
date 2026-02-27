using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI context.
/// </summary>
public class AIContextArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The context resources serialized as JSON.
    /// </summary>
    public JsonElement? Resources { get; set; }
}
