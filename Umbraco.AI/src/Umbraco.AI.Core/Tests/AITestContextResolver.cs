using System.Text.Json;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Resolves mock entity context items for test execution.
/// Converts mock entity JSON directly into AIRequestContextItems
/// that are indistinguishable from real entity context downstream.
/// </summary>
public sealed class AITestContextResolver
{
    /// <summary>
    /// Resolves context items from a mock entity JSON element.
    /// </summary>
    /// <param name="mockEntity">The mock entity JSON (AISerializedEntity structure), or null.</param>
    /// <returns>A list of context items to inject into the execution request.</returns>
    public List<AIRequestContextItem> ResolveContextItems(JsonElement? mockEntity)
    {
        var items = new List<AIRequestContextItem>();

        if (mockEntity.HasValue)
        {
            items.Add(new AIRequestContextItem
            {
                Description = "Currently editing entity (mock)",
                Value = mockEntity.Value.GetRawText()
            });
        }

        return items;
    }
}
