using System.Text.Json;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Serializer for agent type-specific configuration.
/// </summary>
internal static class AIAgentConfigSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Serializes agent config to JSON.
    /// </summary>
    public static string? Serialize(IAIAgentConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(config, config.GetType(), JsonOptions);
    }

    /// <summary>
    /// Deserializes agent config from JSON based on agent type.
    /// </summary>
    public static IAIAgentConfig? Deserialize(AIAgentType agentType, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return agentType switch
            {
                AIAgentType.Standard => new AIStandardAgentConfig(),
                AIAgentType.Orchestrated => new AIOrchestratedAgentConfig(),
                _ => null,
            };
        }

        return agentType switch
        {
            AIAgentType.Standard => JsonSerializer.Deserialize<AIStandardAgentConfig>(json, JsonOptions),
            AIAgentType.Orchestrated => JsonSerializer.Deserialize<AIOrchestratedAgentConfig>(json, JsonOptions),
            _ => null,
        };
    }
}
