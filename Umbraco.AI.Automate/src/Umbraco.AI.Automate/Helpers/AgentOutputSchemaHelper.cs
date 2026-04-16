using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;

namespace Umbraco.AI.Automate.Helpers;

/// <summary>
/// Resolves an agent's configured output schema as a <see cref="JsonSchema"/> for Automate binding.
/// </summary>
internal static class AgentOutputSchemaHelper
{
    /// <summary>
    /// Schema returned when the agent has no structured output configured.
    /// Matches the <c>{ response: "..." }</c> shape produced by <c>TryParseStructuredOutput</c>.
    /// </summary>
    private static readonly JsonSchema FallbackSchema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(
            ("response", new JsonSchemaBuilder().Type(SchemaValueType.String)))
        .Build();

    /// <summary>
    /// Gets the output JSON Schema for an agent. Returns the agent's configured output schema
    /// if available, otherwise returns a fallback schema with a single <c>response</c> string property.
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

        return JsonSchema.FromText(outputSchema.Value.GetRawText());
    }
}
