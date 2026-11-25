using System.Text.Json;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Agent.Deploy.Artifacts;

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
    /// The agent type (e.g., "Standard", "Orchestrated").
    /// </summary>
    public string AgentType { get; set; } = "Standard";

    /// <summary>
    /// Type-specific configuration serialized as JSON.
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// Guardrail IDs for safety and compliance checks (available for all agent types).
    /// </summary>
    public IEnumerable<Guid> GuardrailIds { get; set; } = [];

    /// <summary>
    /// Surface IDs where the agent is available (backoffice, frontend, custom).
    /// </summary>
    public IEnumerable<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Scoping rules serialized as JSON (where the agent is available).
    /// </summary>
    public JsonElement? Scope { get; set; }

    /// <summary>
    /// Starter prompts shown as clickable chips in an empty chat for this agent.
    /// </summary>
    public IEnumerable<AIStarterPrompt> StarterPrompts { get; set; } = [];

    /// <summary>
    /// Aliases of the user groups allowed to use the agent. Empty means everyone. Aliases, not keys,
    /// because a custom user group has a different key on each environment. Artifacts written before
    /// this property existed don't set it and import as everyone.
    /// </summary>
    public IEnumerable<string> AllowedUserGroups { get; set; } = [];

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }
}
