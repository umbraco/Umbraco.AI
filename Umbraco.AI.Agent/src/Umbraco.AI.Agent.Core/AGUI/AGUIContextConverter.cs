using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIContextConverter"/>.
/// </summary>
internal sealed class AGUIContextConverter : IAGUIContextConverter
{
    /// <inheritdoc />
    public IReadOnlyList<AIRequestContextItem> ConvertToRequestContextItems(IEnumerable<AGUIContextItem>? context)
    {
        if (context is null)
        {
            return [];
        }

        return context.Select(item => new AIRequestContextItem
        {
            Description = item.Description,
            Value = item.Value
        }).ToList();
    }
}
