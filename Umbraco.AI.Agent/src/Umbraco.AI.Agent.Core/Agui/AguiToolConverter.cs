using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agui.Models;

namespace Umbraco.AI.Agent.Core.Agui;

/// <inheritdoc />
internal sealed class AguiToolConverter : IAguiToolConverter
{
    /// <inheritdoc />
    public IList<AITool>? ConvertToFrontendTools(IEnumerable<AguiTool>? tools)
    {
        if (tools?.Any() != true)
        {
            return null;
        }

        return tools.Select(t => (AITool)new AIFrontendToolFunction(t)).ToList();
    }
}
