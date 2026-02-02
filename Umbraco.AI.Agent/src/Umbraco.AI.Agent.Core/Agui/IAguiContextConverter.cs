using Umbraco.AI.Agui.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Agui;

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
    IReadOnlyList<AIRequestContextItem> ConvertToRequestContextItems(IEnumerable<AguiContextItem>? context);
}
