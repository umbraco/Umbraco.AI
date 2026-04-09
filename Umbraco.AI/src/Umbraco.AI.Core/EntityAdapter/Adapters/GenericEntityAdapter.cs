using System.Text;
using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Default entity adapter that pretty-prints the entity data as JSON.
/// Used as fallback when no entity-type-specific adapter is registered.
/// </summary>
internal sealed class GenericEntityAdapter : AIEntityAdapterBase
{
    /// <inheritdoc />
    public override string? EntityType => null; // Default adapter

    /// <inheritdoc />
    public override string Name => "Generic";

    /// <inheritdoc />
    public override string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return FormatGeneric(entity);
    }

    /// <summary>
    /// Static formatting method used as fallback by other adapters.
    /// </summary>
    internal static string FormatGeneric(AISerializedEntity entity)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Entity Context");
        sb.AppendLine($"Key: `{entity.Unique}`");
        sb.AppendLine($"Name: `{entity.Name}`");
        sb.AppendLine($"Type: `{entity.EntityType}`");
        sb.AppendLine("**IMPORTANT** When the user says 'this page', 'this document', 'this entity', 'this media item' or similar, you should use this context entry as the reference.");
        sb.AppendLine();
        sb.AppendLine("### Entity Data");
        sb.AppendLine();
        sb.AppendLine("```json");

        var options = new JsonSerializerOptions { WriteIndented = true };
        sb.AppendLine(JsonSerializer.Serialize(entity.Data, options));

        sb.AppendLine("```");

        return sb.ToString();
    }
}
