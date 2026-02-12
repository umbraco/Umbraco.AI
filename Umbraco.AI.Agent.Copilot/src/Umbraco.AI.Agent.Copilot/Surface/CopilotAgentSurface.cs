using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.Surfaces;

namespace Umbraco.AI.Agent.Copilot.Surface;

[AIAgentSurface(SurfaceId, Icon = "icon-chat", SupportedScopeDimensions = [ Constants.AgentScopeDimensions.Section, Constants.AgentScopeDimensions.EntityType ])]
public class CopilotAgentSurface : AIAgentSurfaceBase
{
    /// <summary>
    /// The identifier for the Copilot agent surface.
    /// </summary>
    public const string SurfaceId = "copilot";
}
