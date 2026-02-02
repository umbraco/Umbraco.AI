using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Default implementation of <see cref="IAguiContextConverter"/>.
/// </summary>
internal sealed class AguiContextConverter : IAguiContextConverter
{
    /// <inheritdoc />
    public IReadOnlyList<AiRequestContextItem> ConvertToRequestContextItems(IEnumerable<AguiContextItem>? context)
    {
        if (context is null)
        {
            return [];
        }

        return context.Select(item => new AiRequestContextItem
        {
            Description = item.Description,
            Value = item.Value
        }).ToList();
    }
}
