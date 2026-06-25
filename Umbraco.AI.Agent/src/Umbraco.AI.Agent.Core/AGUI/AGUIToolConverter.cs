using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.AGUI;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <inheritdoc />
internal sealed class AGUIToolConverter : IAGUIToolConverter
{
    /// <inheritdoc />
    public IEnumerable<AIFrontendTool>? ConvertToFrontendTools(IEnumerable<AGUITool>? tools)
    {
        if (tools?.Any() != true)
        {
            return null;
        }

        var frontendTools = new List<AIFrontendTool>();
        foreach (var tool in tools)
        {
            // Vendor metadata travels inline via AGUITool.Metadata per AG-UI spec —
            // scope drives permission grouping, isDestructive drives HITL approval.
            var scope = ReadStringMetadata(tool.Metadata, AGUIConstants.ToolMetadataKeys.Scope);
            var isDestructive = ReadBoolMetadata(tool.Metadata, AGUIConstants.ToolMetadataKeys.IsDestructive);
            frontendTools.Add(new AIFrontendTool(tool, scope, isDestructive));
        }

        return frontendTools;
    }

    private static string? ReadStringMetadata(IReadOnlyDictionary<string, object?>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => raw.ToString(),
        };
    }

    private static bool ReadBoolMetadata(IReadOnlyDictionary<string, object?>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        return raw switch
        {
            bool b => b,
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            string s => bool.TryParse(s, out var parsed) && parsed,
            _ => false,
        };
    }
}
