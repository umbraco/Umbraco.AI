using Umbraco.AI.Agent.Core.Surfaces;

namespace Umbraco.AI.Automate.Surface;

/// <summary>
/// Defines the "automations" surface, enabling agents to be used in Umbraco Automate workflows.
/// </summary>
[AIAgentSurface(SurfaceId, Icon = "icon-nodes")]
public class AutomationsAgentSurface : AIAgentSurfaceBase
{
    /// <summary>
    /// The identifier for the Automations agent surface.
    /// </summary>
    public const string SurfaceId = "automations";
}
