using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <inheritdoc />
internal sealed class AGUIToolConverter : IAGUIToolConverter
{
    /// <inheritdoc />
    public IList<AITool>? ConvertToFrontendTools(IEnumerable<AGUITool>? tools)
    {
        if (tools?.Any() != true)
        {
            return null;
        }

        return tools.Select(t => (AITool)new AIFrontendToolFunction(t)).ToList();
    }
}
