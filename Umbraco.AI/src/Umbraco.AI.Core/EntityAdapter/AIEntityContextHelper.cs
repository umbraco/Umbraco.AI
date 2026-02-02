using System.Text;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Default implementation of <see cref="IAiEntityContextHelper"/>.
/// </summary>
internal sealed class AIEntityContextHelper : IAiEntityContextHelper
{
    /// <inheritdoc />
    public Dictionary<string, object?> BuildContextDictionary(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var context = new Dictionary<string, object?>
        {
            ["entityType"] = entity.EntityType,
            ["entityId"] = entity.Unique,
            ["entityName"] = entity.Name,
            ["contentType"] = entity.ContentType,
        };

        // Add each property value with its alias as key
        foreach (var property in entity.Properties)
        {
            context[property.Alias] = property.Value;
        }

        return context;
    }

    /// <inheritdoc />
    public string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sb = new StringBuilder();

        sb.AppendLine($"## Current Entity Context");
        sb.AppendLine();
        sb.AppendLine($"You are working with a {entity.EntityType} named \"{entity.Name}\".");
        sb.AppendLine($"**IMPORTANT** When the user says 'this page', 'this document', 'this entity', 'this media item' or similar, you should use this context entry as the reference.");

        if (!string.IsNullOrEmpty(entity.ContentType))
        {
            sb.AppendLine($"Content type: {entity.ContentType}");
        }

        if (entity.Properties.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Properties");
            sb.AppendLine();

            foreach (var property in entity.Properties)
            {
                var valueDisplay = property.Value?.ToString() ?? "(empty)";
                sb.AppendLine($"- **{property.Label}** (`{property.Alias}`): {valueDisplay}");
            }
        }

        return sb.ToString();
    }
}
