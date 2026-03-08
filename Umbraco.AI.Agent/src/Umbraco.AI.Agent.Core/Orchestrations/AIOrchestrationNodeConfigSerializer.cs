using System.Text.Json;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Serializer for orchestration node type-specific configuration.
/// Uses <see cref="AIOrchestrationNodeType"/> as the discriminator.
/// </summary>
internal static class AIOrchestrationNodeConfigSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Serializes node config to JSON.
    /// </summary>
    public static string? Serialize(IAIOrchestrationNodeConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(config, config.GetType(), JsonOptions);
    }

    /// <summary>
    /// Deserializes node config from JSON based on node type.
    /// </summary>
    public static IAIOrchestrationNodeConfig Deserialize(AIOrchestrationNodeType nodeType, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return CreateDefault(nodeType);
        }

        return nodeType switch
        {
            AIOrchestrationNodeType.Start => JsonSerializer.Deserialize<AIOrchestrationStartNodeConfig>(json, JsonOptions) ?? new AIOrchestrationStartNodeConfig(),
            AIOrchestrationNodeType.End => JsonSerializer.Deserialize<AIOrchestrationEndNodeConfig>(json, JsonOptions) ?? new AIOrchestrationEndNodeConfig(),
            AIOrchestrationNodeType.Agent => JsonSerializer.Deserialize<AIOrchestrationAgentNodeConfig>(json, JsonOptions) ?? new AIOrchestrationAgentNodeConfig(),
            AIOrchestrationNodeType.ToolCall => JsonSerializer.Deserialize<AIOrchestrationToolCallNodeConfig>(json, JsonOptions) ?? new AIOrchestrationToolCallNodeConfig(),
            AIOrchestrationNodeType.Router => JsonSerializer.Deserialize<AIOrchestrationRouterNodeConfig>(json, JsonOptions) ?? new AIOrchestrationRouterNodeConfig(),
            AIOrchestrationNodeType.Aggregator => JsonSerializer.Deserialize<AIOrchestrationAggregatorNodeConfig>(json, JsonOptions) ?? new AIOrchestrationAggregatorNodeConfig(),
            AIOrchestrationNodeType.CommunicationBus => JsonSerializer.Deserialize<AIOrchestrationCommunicationBusNodeConfig>(json, JsonOptions) ?? new AIOrchestrationCommunicationBusNodeConfig(),
            _ => new AIOrchestrationStartNodeConfig(),
        };
    }

    /// <summary>
    /// Creates a default config instance for the given node type.
    /// </summary>
    public static IAIOrchestrationNodeConfig CreateDefault(AIOrchestrationNodeType nodeType)
    {
        return nodeType switch
        {
            AIOrchestrationNodeType.Start => new AIOrchestrationStartNodeConfig(),
            AIOrchestrationNodeType.End => new AIOrchestrationEndNodeConfig(),
            AIOrchestrationNodeType.Agent => new AIOrchestrationAgentNodeConfig(),
            AIOrchestrationNodeType.ToolCall => new AIOrchestrationToolCallNodeConfig(),
            AIOrchestrationNodeType.Router => new AIOrchestrationRouterNodeConfig(),
            AIOrchestrationNodeType.Aggregator => new AIOrchestrationAggregatorNodeConfig(),
            AIOrchestrationNodeType.CommunicationBus => new AIOrchestrationCommunicationBusNodeConfig(),
            _ => new AIOrchestrationStartNodeConfig(),
        };
    }
}
