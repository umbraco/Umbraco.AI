using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agent.Core.Agui;

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

        return tools.Select(t => (AITool)new AiFrontendToolFunction(t)).ToList();
    }
}
