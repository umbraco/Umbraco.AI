using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Converts between AG-UI context items and core runtime context items.
/// </summary>
public interface IAguiContextConverter
{
    /// <summary>
    /// Converts AG-UI context items to core runtime context items.
    /// </summary>
    /// <param name="context">The AG-UI context items to convert.</param>
    /// <returns>A list of core runtime context items.</returns>
    IReadOnlyList<AiRuntimeContextItem> ConvertToRuntimeContextItems(IEnumerable<AguiContextItem>? context);
}
