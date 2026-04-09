using System.Text.Json;
using Json.Schema;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;

namespace Umbraco.AI.Automate.Helpers;

/// <summary>
/// Resolves an agent's configured output schema as a <see cref="JsonSchema"/> for Automate binding.
/// </summary>
internal static class AgentOutputSchemaHelper
{
    /// <summary>
    /// Gets the output JSON Schema for an agent, or null if the agent has no structured output configured.
    /// </summary>
    internal static async Task<JsonSchema?> GetOutputSchemaAsync(
        IAIAgentService agentService,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var agent = await agentService.GetAgentAsync(agentId, cancellationToken);
        JsonElement? outputSchema = agent?.GetStandardConfig()?.OutputSchema;

        if (outputSchema is null)
        {
            return null;
        }

        return JsonSchema.FromText(outputSchema.Value.GetRawText());
    }
}
