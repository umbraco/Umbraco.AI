using System.Text.Json;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Core.RuntimeContext;

/// <summary>
/// Extracts surface identifier from context items and stores it in runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Frontends can send surface information via context items in the format:
/// <code>{ "surface": "copilot" }</code>
/// </para>
/// <para>
/// The extracted surface is stored in <see cref="Constants.ContextKeys.Surface"/>
/// and is used for filtering agents by which UI surface they're available in.
/// </para>
/// </remarks>
internal sealed class SurfaceContextContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Umbraco.AI.Core.Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.Handle(
            IsSurfaceContext,
            item => ProcessSurfaceContext(item, context));
    }

    private bool IsSurfaceContext(AIRequestContextItem item)
    {
        // Check if the value contains a surface property
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("surface", out _);
        }
        catch
        {
            return false;
        }
    }

    private void ProcessSurfaceContext(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            if (value.TryGetProperty("surface", out var surfaceElement))
            {
                var surface = surfaceElement.GetString();
                if (!string.IsNullOrEmpty(surface))
                {
                    // Store in runtime context for agent filtering
                    context.SetValue(Constants.ContextKeys.Surface, surface);

                    // Note: No system message needed - surface is for agent filtering, not LLM context
                }
            }
        }
        catch
        {
            // Silently ignore deserialization errors
        }
    }
}
