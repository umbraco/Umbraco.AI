using System.Text.Json;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Extracts section pathname from context items and stores it in runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Frontends can send section information via context items in the format:
/// <code>{ "section": "content" }</code>
/// </para>
/// <para>
/// The extracted section is stored in <see cref="Constants.ContextKeys.Section"/>
/// and can be used for context-aware filtering.
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
        // Check if the value contains a section property
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("section", out _);
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
            if (value.TryGetProperty("section", out var sectionElement))
            {
                var section = sectionElement.GetString();
                if (!string.IsNullOrEmpty(section))
                {
                    // Store in data bag
                    context.SetValue(Constants.ContextKeys.Section, section);

                    // Add system message to inform the LLM
                    context.SystemMessageParts.Add(FormatSectionContext(section));
                }
            }
        }
        catch
        {
            // Silently ignore deserialization errors
        }
    }

    private static string FormatSectionContext(string section)
    {
        return $"## Current Section\nThe user is currently in the '{section}' section.";
    }
}
