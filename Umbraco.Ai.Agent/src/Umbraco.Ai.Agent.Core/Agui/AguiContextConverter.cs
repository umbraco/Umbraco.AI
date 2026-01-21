using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Default implementation of <see cref="IAguiContextConverter"/>.
/// </summary>
public sealed class AguiContextConverter : IAguiContextConverter
{
    /// <inheritdoc />
    public IReadOnlyList<AiRuntimeContextItem> ConvertToRuntimeContextItems(IEnumerable<AguiContextItem>? context)
    {
        if (context is null)
        {
            return [];
        }

        return context.Select(item => new AiRuntimeContextItem
        {
            Description = item.Description,
            Value = item.Value
        }).ToList();
    }
}
