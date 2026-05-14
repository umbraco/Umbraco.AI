using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <inheritdoc />
internal sealed class AGUIToolConverter : IAGUIToolConverter
{
    private readonly ILoggerFactory? _loggerFactory;

    public AGUIToolConverter(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IList<AITool>? ConvertToFrontendTools(IEnumerable<AGUITool>? tools)
    {
        if (tools?.Any() != true)
        {
            return null;
        }

        return tools.Select(t => (AITool)new AIFrontendToolFunction(t, loggerFactory: _loggerFactory)).ToList();
    }
}
