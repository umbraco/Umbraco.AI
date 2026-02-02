using Umbraco.AI.Agui.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Agui;

/// <summary>
/// Default implementation of <see cref="IAguiContextConverter"/>.
/// </summary>
internal sealed class AguiContextConverter : IAguiContextConverter
{
    /// <inheritdoc />
    public IReadOnlyList<AIRequestContextItem> ConvertToRequestContextItems(IEnumerable<AguiContextItem>? context)
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
