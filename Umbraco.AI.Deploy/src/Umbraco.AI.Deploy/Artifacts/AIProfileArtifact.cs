using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI profile.
/// </summary>
public class AIProfileArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The capability type as an integer (maps to AICapability enum).
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// The provider identifier for the model.
    /// </summary>
    public required string ModelProviderId { get; set; }

    /// <summary>
    /// The model identifier.
    /// </summary>
    public required string ModelModelId { get; set; }

    /// <summary>
    /// The UDI of the connection this profile uses.
    /// </summary>
    public required GuidUdi ConnectionUdi { get; set; }

    /// <summary>
    /// Profile-specific settings serialized as JSON.
    /// </summary>
    public JsonElement? Settings { get; set; }

    /// <summary>
    /// Tags for categorizing the profile.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];
}
