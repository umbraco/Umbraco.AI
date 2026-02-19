using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI connection.
/// </summary>
public class AIConnectionArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The provider identifier for this connection.
    /// </summary>
    public required string ProviderId { get; set; }

    /// <summary>
    /// The connection settings, filtered according to deployment configuration.
    /// </summary>
    public JsonElement? Settings { get; set; }

    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    public bool IsActive { get; set; }
}
