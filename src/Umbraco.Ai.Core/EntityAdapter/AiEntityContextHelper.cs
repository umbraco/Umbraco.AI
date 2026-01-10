using System.Text;

namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Default implementation of <see cref="IAiEntityContextHelper"/>.
/// </summary>
internal sealed class AiEntityContextHelper : IAiEntityContextHelper
{
    /// <inheritdoc />
    public Dictionary<string, object?> BuildContextDictionary(AiSerializedEntity entity)
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
            context[$"property_{property.Alias}"] = property.Value;
        }

        return context;
    }

    /// <inheritdoc />
    public string FormatForLlm(AiSerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sb = new StringBuilder();

        sb.AppendLine($"## Current Entity Context");
        sb.AppendLine();
        sb.AppendLine($"You are working with a {entity.EntityType} named \"{entity.Name}\".");

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
                if (valueDisplay.Length > 200)
                {
                    valueDisplay = valueDisplay[..197] + "...";
                }

                sb.AppendLine($"- **{property.Label}** (`{property.Alias}`): {valueDisplay}");
            }
        }

        return sb.ToString();
    }
}
