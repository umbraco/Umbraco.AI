using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Converts AG-UI tool definitions from a run request into <see cref="AIFrontendTool"/>
/// instances, lifting vendor metadata (scope, isDestructive) off <see cref="AGUITool.Metadata"/>.
/// </summary>
public interface IAGUIToolConverter
{
    /// <summary>
    /// Maps AG-UI tools to <see cref="AIFrontendTool"/>, reading <c>scope</c> and
    /// <c>isDestructive</c> from each tool's inline <see cref="AGUITool.Metadata"/>.
    /// </summary>
    /// <param name="tools">The AG-UI tools from the run request.</param>
    /// <returns>The mapped frontend tools, or <c>null</c> when <paramref name="tools"/> is null/empty.</returns>
    IEnumerable<AIFrontendTool>? ConvertToFrontendTools(IEnumerable<AGUITool>? tools);
}
