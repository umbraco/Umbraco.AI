using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Automate.Actions;

namespace Umbraco.AI.Automate.Helpers;

/// <summary>
/// Resolves an agent's configured output schema as a <see cref="JsonSchema"/> for Automate binding.
/// </summary>
internal static class AgentOutputSchemaHelper
{
    /// <summary>
    /// Schema returned when the agent has no structured output configured.
    /// Matches the <c>{ response: "..." }</c> shape produced by <c>BuildOutputData</c>.
    /// </summary>
    private static readonly JsonSchema FallbackSchema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(
            (RunAgentAction.RawResponseKey, new JsonSchemaBuilder().Type(SchemaValueType.String)))
        .Build();

    /// <summary>
    /// Gets the output JSON Schema for an agent. The raw <c>response</c> string property is always
    /// included so a bindable property is available even when the agent has no structured output
    /// (or returns text that doesn't match its schema). When the agent has a structured output
    /// schema, its properties are merged alongside the raw <c>response</c> property.
    /// </summary>
    internal static async Task<JsonSchema> GetOutputSchemaAsync(
        IAIAgentService agentService,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var agent = await agentService.GetAgentAsync(agentId, cancellationToken);
        JsonElement? outputSchema = agent?.GetStandardConfig()?.OutputSchema;

        if (outputSchema is null)
        {
            return FallbackSchema;
        }

        // Merge the agent's structured schema with the always-present raw `response` property.
        if (JsonNode.Parse(outputSchema.Value.GetRawText()) is not JsonObject schemaObject)
        {
            return FallbackSchema;
        }

        if (schemaObject["properties"] is not JsonObject properties)
        {
            properties = new JsonObject();
            schemaObject["properties"] = properties;
        }

        // The reserved raw response is always a string and always takes this slot, so downstream
        // steps can rely on `response` regardless of what the structured schema declares.
        properties[RunAgentAction.RawResponseKey] = new JsonObject { ["type"] = "string" };

        return JsonSchema.FromText(schemaObject.ToJsonString());
    }
}
