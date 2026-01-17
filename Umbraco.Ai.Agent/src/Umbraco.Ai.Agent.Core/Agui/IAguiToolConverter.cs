using Microsoft.Extensions.AI;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Converts between AG-UI tool definitions and M.E.AI tool types.
/// </summary>
public interface IAguiToolConverter
{
    /// <summary>
    /// Converts AG-UI tools to M.E.AI AITool format as frontend tools.
    /// These tools expose their schema to the LLM but execution happens on the client.
    /// </summary>
    /// <param name="tools">The AG-UI tools to convert.</param>
    /// <returns>A list of AITool instances, or null if no tools provided.</returns>
    IList<AITool>? ConvertToFrontendTools(IEnumerable<AguiTool>? tools);
}
