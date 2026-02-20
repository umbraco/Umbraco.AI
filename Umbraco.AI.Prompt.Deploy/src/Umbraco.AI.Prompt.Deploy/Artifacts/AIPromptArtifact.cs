using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Prompt.Deploy.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI prompt template.
/// </summary>
public class AIPromptArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// Optional description of what the prompt does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The system instructions for the prompt.
    /// </summary>
    public required string Instructions { get; set; }

    /// <summary>
    /// The UDI of the profile this prompt uses (optional).
    /// </summary>
    public GuidUdi? ProfileUdi { get; set; }

    /// <summary>
    /// Context IDs that provide additional information to the prompt.
    /// </summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Tags for categorizing the prompt.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether to include entity-specific context when executing the prompt.
    /// </summary>
    public bool IncludeEntityContext { get; set; }

    /// <summary>
    /// Number of response options to generate.
    /// </summary>
    public int OptionCount { get; set; }

    /// <summary>
    /// Scoping rules serialized as JSON (where the prompt is available).
    /// </summary>
    public JsonElement? Scope { get; set; }
}
