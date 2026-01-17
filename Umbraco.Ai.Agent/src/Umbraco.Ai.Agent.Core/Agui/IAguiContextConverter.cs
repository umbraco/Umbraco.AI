using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Core.RequestContext;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Converts between AG-UI context items and core request context items.
/// </summary>
public interface IAguiContextConverter
{
    /// <summary>
    /// Converts AG-UI context items to core request context items.
    /// </summary>
    /// <param name="context">The AG-UI context items to convert.</param>
    /// <returns>A list of core request context items.</returns>
    IReadOnlyList<AiRequestContextItem> ConvertToRequestContextItems(IEnumerable<AguiContextItem>? context);
}
