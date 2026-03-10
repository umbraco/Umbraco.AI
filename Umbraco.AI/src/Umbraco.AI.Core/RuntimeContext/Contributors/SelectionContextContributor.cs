using System.Text.Json;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Extracts selection text from context items and makes it available as a template variable.
/// </summary>
/// <remarks>
/// Frontends can send selection data via context items in the format:
/// <code>{ "selection": "the selected text" }</code>
/// The extracted value is stored as the <c>selection</c> template variable
/// for use in prompt instructions via <c>{{selection}}</c>.
/// </remarks>
internal sealed class SelectionContextContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.Handle(
            IsSelectionContext,
            item => ProcessSelectionContext(item, context));
    }

    private bool IsSelectionContext(AIRequestContextItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("selection", out _);
        }
        catch
        {
            return false;
        }
    }

    private void ProcessSelectionContext(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            if (value.TryGetProperty("selection", out var selectionElement))
            {
                var selection = selectionElement.GetString();
                if (!string.IsNullOrEmpty(selection))
                {
                    context.Variables["selection"] = selection;
                }
            }
        }
        catch
        {
            // Silently ignore deserialization errors
        }
    }
}
