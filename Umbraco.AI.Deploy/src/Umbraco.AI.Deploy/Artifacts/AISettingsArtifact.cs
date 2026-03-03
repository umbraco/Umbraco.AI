using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for AI settings (default profile configuration).
/// </summary>
public class AISettingsArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The UDI of the default chat profile (optional).
    /// </summary>
    public GuidUdi? DefaultChatProfileUdi { get; set; }

    /// <summary>
    /// The UDI of the default embedding profile (optional).
    /// </summary>
    public GuidUdi? DefaultEmbeddingProfileUdi { get; set; }
}
