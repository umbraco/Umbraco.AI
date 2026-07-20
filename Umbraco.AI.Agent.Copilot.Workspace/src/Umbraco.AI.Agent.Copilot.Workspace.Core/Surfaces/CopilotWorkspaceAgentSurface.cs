using Umbraco.AI.Agent.Core.Surfaces;

namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces;

/// <summary>
/// The Copilot Workspace agent surface — a broad, system-wide chat surface.
/// </summary>
/// <remarks>
/// <see cref="AIAgentSurfaceBase.SupportedScopeDimensions"/> is intentionally empty: the Workspace is
/// unscoped (not tied to a section/entity), so agents opt in purely via their <c>SurfaceIds</c>.
/// </remarks>
[AIAgentSurface(SurfaceId, Icon = "icon-chat", SupportedScopeDimensions = [])]
public class CopilotWorkspaceAgentSurface : AIAgentSurfaceBase
{
    /// <summary>
    /// The identifier for the Copilot Workspace agent surface.
    /// </summary>
    public const string SurfaceId = "copilot-workspace";
}
