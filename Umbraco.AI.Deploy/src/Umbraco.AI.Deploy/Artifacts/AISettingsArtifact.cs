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

    /// <summary>
    /// The UDI of the default speech-to-text profile (optional).
    /// </summary>
    public GuidUdi? DefaultSpeechToTextProfileUdi { get; set; }

    /// <summary>
    /// The UDI of the default image-generation profile (optional).
    /// </summary>
    public GuidUdi? DefaultImageGenerationProfileUdi { get; set; }

    /// <summary>
    /// The UDI of the default decision profile (optional).
    /// </summary>
    public GuidUdi? DefaultDecisionProfileUdi { get; set; }

    /// <summary>
    /// The UDI of the classifier chat profile (optional).
    /// </summary>
    public GuidUdi? ClassifierChatProfileUdi { get; set; }

    /// <summary>
    /// How the AI-generated disclosure notice is shown (Always, Dismissible, Off).
    /// Null for artifacts written before this setting existed, in which case the target keeps its value.
    /// </summary>
    public string? DisclosureNoticeMode { get; set; }
}
