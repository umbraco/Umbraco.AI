using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Default entity formatter that pretty-prints the entity data as JSON.
/// Used as fallback when no entity-type-specific formatter is registered.
/// </summary>
internal sealed class AIGenericEntityFormatter : IAIEntityFormatter
{
    /// <inheritdoc />
    public string? EntityType => null; // Default formatter

    /// <inheritdoc />
    public string Format(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sb = new StringBuilder();

        sb.AppendLine($"## Current Entity Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        sb.AppendLine($"Name: `{entity.Name}`");
        sb.AppendLine($"Type: `{entity.EntityType}`");
        sb.AppendLine($"**IMPORTANT** When the user says 'this page', 'this document', 'this entity', 'this media item' or similar, you should use this context entry as the reference.");
        sb.AppendLine();
        sb.AppendLine("### Entity Data");
        sb.AppendLine();
        sb.AppendLine("```json");

        // Pretty-print the Data JsonElement
        var options = new JsonSerializerOptions { WriteIndented = true };
        sb.AppendLine(JsonSerializer.Serialize(entity.Data, options));

        sb.AppendLine("```");

        return sb.ToString();
    }
}
