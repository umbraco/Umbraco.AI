using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Agent.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI orchestration.
/// </summary>
public class AIOrchestrationArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// Optional description of the orchestration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The UDI of the profile this orchestration uses (optional).
    /// </summary>
    public GuidUdi? ProfileUdi { get; set; }

    /// <summary>
    /// Surface IDs where the orchestration is available.
    /// </summary>
    public IEnumerable<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Scoping rules serialized as JSON (where the orchestration is available).
    /// </summary>
    public JsonElement? Scope { get; set; }

    /// <summary>
    /// The workflow graph serialized as JSON.
    /// </summary>
    public JsonElement? Graph { get; set; }

    /// <summary>
    /// Whether the orchestration is active.
    /// </summary>
    public bool IsActive { get; set; }
}
