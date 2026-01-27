using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.DevUI.Models;

using ChatClientAgent = Microsoft.Agents.AI.ChatClientAgent;

namespace Umbraco.Ai.DevUI.Services;

/// <summary>
/// Service for discovering and providing entity information for DevUI.
/// Combines framework agents and Umbraco.Ai agents for runtime discovery.
/// </summary>
public class DevUIEntityDiscoveryService : IDevUIEntityDiscoveryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAiAgentService _agentService;
    private readonly IAiAgentFactory _agentFactory;

    public DevUIEntityDiscoveryService(
        IServiceProvider serviceProvider,
        IAiAgentService agentService,
        IAiAgentFactory agentFactory)
    {
        _serviceProvider = serviceProvider;
        _agentService = agentService;
        _agentFactory = agentFactory;
    }

    public async Task<DevUIDiscoveryResponse> GetAllEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new Dictionary<string, DevUIEntityInfo>();

        // Get framework agents (if any are registered besides our fallback)
        var frameworkAgents = _serviceProvider.GetKeyedServices<AIAgent>(KeyedService.AnyKey)
            .Where(a => a != null);

        foreach (var agent in frameworkAgents)
        {
            // Skip if this is a Umbraco agent (avoid duplicates from fallback factory)
            if (await IsUmbracoAgentAsync(agent.Name, cancellationToken))
                continue;

            entities[agent.Name ?? agent.Id] = CreateEntityInfoFromFrameworkAgent(agent);
        }

        // Get Umbraco.Ai agents dynamically (runtime discovery!)
        var umbracoAgents = await _agentService.GetAgentsAsync(cancellationToken);
        foreach (var agent in umbracoAgents)
        {
            entities[agent.Alias] = await CreateEntityInfoFromUmbracoAgentAsync(agent, cancellationToken);
        }

        return new DevUIDiscoveryResponse(entities.Values.OrderBy(e => e.Id).ToList());
    }

    public async Task<DevUIEntityInfo?> GetEntityInfoAsync(string entityId, CancellationToken cancellationToken = default)
    {
        // Try framework agents first
        var frameworkAgent = _serviceProvider.GetKeyedService<AIAgent>(entityId);
        if (frameworkAgent != null && !await IsUmbracoAgentAsync(entityId, cancellationToken))
        {
            return CreateEntityInfoFromFrameworkAgent(frameworkAgent);
        }

        // Try Umbraco agents
        var umbracoAgents = await _agentService.GetAgentsAsync(cancellationToken);
        var agent = umbracoAgents.FirstOrDefault(a => a.Alias.Equals(entityId, StringComparison.Ordinal));

        if (agent != null)
        {
            return await CreateEntityInfoFromUmbracoAgentAsync(agent, cancellationToken);
        }

        return null;
    }

    private async Task<bool> IsUmbracoAgentAsync(string? agentName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(agentName))
            return false;

        var agents = await _agentService.GetAgentsAsync(cancellationToken);
        return agents.Any(a => a.Alias.Equals(agentName, StringComparison.Ordinal));
    }

    private static DevUIEntityInfo CreateEntityInfoFromFrameworkAgent(AIAgent agent)
    {
        var tools = new List<string>();
        var metadata = new Dictionary<string, JsonElement>();

        // Extract tools using ChatOptions
        if (agent.GetService<ChatOptions>() is { Tools: { Count: > 0 } agentTools })
        {
            tools.AddRange(agentTools
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name!));
        }

        // Extract model info
        string? modelId = null;
        string? chatClientType = null;
        string? instructions = null;

        if (agent.GetService<IChatClient>() is { } chatClient)
        {
            chatClientType = chatClient.GetType().Name;
            if (chatClient.GetService<ChatClientMetadata>() is { } chatClientMetadata)
            {
                modelId = chatClientMetadata.DefaultModelId;
                if (!string.IsNullOrWhiteSpace(chatClientMetadata.ProviderName))
                {
                    metadata["chat_client_provider"] = JsonSerializer.SerializeToElement(
                        chatClientMetadata.ProviderName);
                }
            }
        }

        // Get instructions from ChatClientAgent
        if (agent is ChatClientAgent chatAgent && !string.IsNullOrWhiteSpace(chatAgent.Instructions))
        {
            instructions = chatAgent.Instructions;
        }

        metadata["agent_type"] = JsonSerializer.SerializeToElement(agent.GetType().Name);

        return new DevUIEntityInfo(
            Id: agent.Name ?? agent.Id,
            Type: "agent",
            Name: agent.Name ?? agent.Id,
            Description: agent.Description,
            Framework: "agent_framework",
            Tools: tools,
            Metadata: metadata)
        {
            Source = "framework",
            Instructions = instructions,
            ModelId = modelId,
            ChatClientType = chatClientType,
            Executors = []
        };
    }

    private async Task<DevUIEntityInfo> CreateEntityInfoFromUmbracoAgentAsync(
        AiAgent agent,
        CancellationToken cancellationToken)
    {
        var tools = new List<string>();
        var metadata = new Dictionary<string, JsonElement>
        {
            ["provider_name"] = JsonSerializer.SerializeToElement("Umbraco.Ai"),
            ["agent_type"] = JsonSerializer.SerializeToElement("UmbracoAiAgent")
        };

        if (agent.ProfileId.HasValue)
        {
            metadata["profile_id"] = JsonSerializer.SerializeToElement(agent.ProfileId.Value);
        }

        // Try to get agent details to extract tools
        try
        {
            var mafAgent = await _agentFactory.CreateAgentAsync(agent, additionalTools: null,
                additionalProperties: null, cancellationToken: cancellationToken);

            // Extract tools using ChatOptions
            if (mafAgent.GetService<ChatOptions>() is { Tools: { Count: > 0 } agentTools })
            {
                tools.AddRange(agentTools
                    .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                    .Select(t => t.Name!));
            }

            // Get model info from chat client
            if (mafAgent.GetService<IChatClient>() is { } chatClient)
            {
                metadata["chat_client_type"] = JsonSerializer.SerializeToElement(chatClient.GetType().Name);

                if (chatClient.GetService<ChatClientMetadata>() is { } chatClientMetadata)
                {
                    if (!string.IsNullOrWhiteSpace(chatClientMetadata.DefaultModelId))
                    {
                        metadata["model_id"] = JsonSerializer.SerializeToElement(chatClientMetadata.DefaultModelId);
                    }
                    if (!string.IsNullOrWhiteSpace(chatClientMetadata.ProviderName))
                    {
                        metadata["chat_client_provider"] = JsonSerializer.SerializeToElement(
                            chatClientMetadata.ProviderName);
                    }
                }
            }
        }
        catch
        {
            // If we can't create the agent, just return basic info
        }

        return new DevUIEntityInfo(
            Id: agent.Alias,
            Type: "agent",
            Name: agent.Name,
            Description: agent.Description,
            Framework: "umbraco_ai",
            Tools: tools,
            Metadata: metadata)
        {
            Source = "umbraco",
            Instructions = agent.Instructions
        };
    }
}
