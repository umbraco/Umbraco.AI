using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Converts between AG-UI context items and core request context items.
/// </summary>
public interface IAGUIContextConverter
{
    /// <summary>
    /// Converts AG-UI context items to core request context items.
    /// </summary>
    /// <param name="context">The AG-UI context items to convert.</param>
    /// <returns>A list of core request context items.</returns>
    IReadOnlyList<AIRequestContextItem> ConvertToRequestContextItems(IEnumerable<AGUIContextItem>? context);
}
