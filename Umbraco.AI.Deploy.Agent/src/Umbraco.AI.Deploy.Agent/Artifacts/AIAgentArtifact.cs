using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Agent.Artifacts;

/// <summary>
/// Represents a deployment artifact for an AI agent.
/// </summary>
public class AIAgentArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// Optional description of what the agent does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The UDI of the profile this agent uses (optional).
    /// </summary>
    public GuidUdi? ProfileUdi { get; set; }

    /// <summary>
    /// Context IDs that provide additional information to the agent.
    /// </summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Surface IDs where the agent is available (backoffice, frontend, custom).
    /// </summary>
    public IEnumerable<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Scoping rules serialized as JSON (where the agent is available).
    /// </summary>
    public JsonElement? Scope { get; set; }

    /// <summary>
    /// Specific tool IDs the agent is allowed to use.
    /// </summary>
    public IEnumerable<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scope IDs the agent is allowed to access (e.g., "content-read", "media-write").
    /// </summary>
    public IEnumerable<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// Per-user-group permission overrides serialized as JSON.
    /// </summary>
    public JsonElement? UserGroupPermissions { get; set; }

    /// <summary>
    /// Optional system instructions for the agent.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }
}
