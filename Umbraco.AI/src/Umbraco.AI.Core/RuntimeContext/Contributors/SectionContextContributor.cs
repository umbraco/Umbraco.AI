using System.Text.Json;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Extracts section pathname from context items and stores it in runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Frontends can send section information via context items in the format:
/// <code>{ "sectionAlias": "content" }</code>
/// </para>
/// <para>
/// The extracted section is stored in <see cref="Constants.ContextKeys.SectionAlias"/>
/// and can be used for context-aware tool filtering.
/// </para>
/// </remarks>
internal sealed class SectionContextContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.Handle(
            IsSectionContext,
            item => ProcessSectionContext(item, context));
    }

    private bool IsSectionContext(AIRequestContextItem item)
    {
        // Check if the value contains a sectionAlias property
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("sectionAlias", out _);
        }
        catch
        {
            return false;
        }
    }

    private void ProcessSectionContext(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            if (value.TryGetProperty("sectionAlias", out var sectionElement))
            {
                var sectionAlias = sectionElement.GetString();
                if (!string.IsNullOrEmpty(sectionAlias))
                {
                    // Store in data bag
                    context.SetValue(Constants.ContextKeys.SectionAlias, sectionAlias);

                    // Add system message to inform the LLM
                    context.SystemMessageParts.Add(FormatSectionContext(sectionAlias));
                }
            }
        }
        catch
        {
            // Silently ignore deserialization errors
        }
    }

    private static string FormatSectionContext(string sectionAlias)
    {
        return $"## Current Section\nThe user is currently in the '{sectionAlias}' section.";
    }
}
