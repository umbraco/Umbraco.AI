using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Tools;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Builds a MAF workflow agent from an orchestrated agent's graph.
/// </summary>
internal sealed class AIOrchestrationExecutor : IAIOrchestrationExecutor
{
    private readonly IAIAgentService _agentService;
    private readonly IAIAgentFactory _agentFactory;
    private readonly IAIProfileService _profileService;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly AIToolCollection _toolCollection;
    private readonly IAIFunctionFactory _functionFactory;
    private readonly ILogger<AIOrchestrationExecutor> _logger;

    public AIOrchestrationExecutor(
        IAIAgentService agentService,
        IAIAgentFactory agentFactory,
        IAIProfileService profileService,
        IAIChatClientFactory chatClientFactory,
        AIToolCollection toolCollection,
        IAIFunctionFactory functionFactory,
        ILogger<AIOrchestrationExecutor> logger)
    {
        _agentService = agentService;
        _agentFactory = agentFactory;
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
        _toolCollection = toolCollection;
        _functionFactory = functionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MsAIAgent> BuildWorkflowAgentAsync(
        Agents.AIAgent agent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var config = agent.GetOrchestratedConfig()
            ?? throw new InvalidOperationException($"Agent '{agent.Alias}' is not an orchestrated agent.");

        var graph = config.Graph;

        // Find start node
        var startNode = graph.Nodes.FirstOrDefault(n => n.Type == AIOrchestrationNodeType.Start)
            ?? throw new InvalidOperationException("Orchestration graph must have a Start node.");

        // Build agent lookup for the graph
        var agentLookup = await BuildAgentLookupAsync(graph, cancellationToken);

        // Build the execution pipeline starting from the Start node
        var pipeline = BuildPipeline(graph, startNode, agentLookup);

        // Wrap the pipeline as a single composite agent
        return new OrchestrationPipelineAgent(
            agent.Name,
            agent.Description,
            pipeline);
    }

    /// <summary>
    /// Creates MAF agent instances for all Agent-type nodes in the graph.
    /// </summary>
    private async Task<Dictionary<string, MsAIAgent>> BuildAgentLookupAsync(
        AIOrchestrationGraph graph,
        CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, MsAIAgent>();

        foreach (var node in graph.Nodes.Where(n => n.Type == AIOrchestrationNodeType.Agent))
        {
            var agentConfig = node.Config as AIOrchestrationAgentNodeConfig;
            if (agentConfig?.AgentId is null)
            {
                _logger.LogWarning("Agent node '{NodeId}' has no AgentId configured, skipping", node.Id);
                continue;
            }

            var agentDefinition = await _agentService.GetAgentAsync(agentConfig.AgentId.Value, cancellationToken);
            if (agentDefinition is null)
            {
                _logger.LogWarning("Agent '{AgentId}' referenced by node '{NodeId}' not found, skipping", agentConfig.AgentId, node.Id);
                continue;
            }

            var mafAgent = await _agentFactory.CreateAgentAsync(agentDefinition, cancellationToken: cancellationToken);
            lookup[node.Id] = mafAgent;
        }

        return lookup;
    }

    /// <summary>
    /// Builds the execution pipeline from the graph, starting at the given node.
    /// </summary>
    private List<OrchestrationStep> BuildPipeline(
        AIOrchestrationGraph graph,
        AIOrchestrationNode startNode,
        Dictionary<string, MsAIAgent> agentLookup)
    {
        var steps = new List<OrchestrationStep>();
        var visited = new HashSet<string>();
        var queue = new Queue<AIOrchestrationNode>();

        // Get successors of Start node
        foreach (var successor in GetSuccessors(graph, startNode.Id))
        {
            queue.Enqueue(successor);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.Id)) continue;

            var step = CreateStep(graph, node, agentLookup);
            if (step is not null)
            {
                steps.Add(step);
            }

            // Follow edges to next nodes (unless it's an End node)
            if (node.Type != AIOrchestrationNodeType.End)
            {
                foreach (var successor in GetSuccessors(graph, node.Id))
                {
                    if (!visited.Contains(successor.Id))
                    {
                        queue.Enqueue(successor);
                    }
                }
            }
        }

        return steps;
    }

    /// <summary>
    /// Creates an execution step for a single graph node.
    /// </summary>
    private OrchestrationStep? CreateStep(
        AIOrchestrationGraph graph,
        AIOrchestrationNode node,
        Dictionary<string, MsAIAgent> agentLookup)
    {
        return node.Type switch
        {
            AIOrchestrationNodeType.Agent => CreateAgentStep(node, agentLookup),
            AIOrchestrationNodeType.ToolCall => CreateToolCallStep(node),
            AIOrchestrationNodeType.Router => CreateRouterStep(graph, node),
            AIOrchestrationNodeType.Aggregator => CreateAggregatorStep(node),
            AIOrchestrationNodeType.CommunicationBus => CreateCommunicationBusStep(graph, node, agentLookup),
            AIOrchestrationNodeType.End => new EndOrchestrationStep(node.Id, node.Label),
            _ => null,
        };
    }

    private AgentOrchestrationStep? CreateAgentStep(
        AIOrchestrationNode node,
        Dictionary<string, MsAIAgent> agentLookup)
    {
        if (!agentLookup.TryGetValue(node.Id, out var mafAgent))
        {
            return null;
        }

        var config = node.Config as AIOrchestrationAgentNodeConfig;
        return new AgentOrchestrationStep(node.Id, node.Label, mafAgent, config?.IsManager ?? false);
    }

    private ToolCallOrchestrationStep? CreateToolCallStep(AIOrchestrationNode node)
    {
        var config = node.Config as AIOrchestrationToolCallNodeConfig;
        if (string.IsNullOrEmpty(config?.ToolId))
        {
            return null;
        }

        return new ToolCallOrchestrationStep(node.Id, node.Label, config.ToolId);
    }

    private RouterOrchestrationStep CreateRouterStep(AIOrchestrationGraph graph, AIOrchestrationNode node)
    {
        var outgoingEdges = graph.Edges
            .Where(e => e.SourceNodeId == node.Id)
            .OrderBy(e => e.Priority ?? int.MaxValue)
            .ToList();

        return new RouterOrchestrationStep(node.Id, node.Label, outgoingEdges);
    }

    private AggregatorOrchestrationStep CreateAggregatorStep(AIOrchestrationNode node)
    {
        var config = node.Config as AIOrchestrationAggregatorNodeConfig;
        return new AggregatorOrchestrationStep(
            node.Id,
            node.Label,
            config?.AggregationStrategy ?? AIOrchestrationAggregationStrategy.Concat,
            config?.ProfileId);
    }

    private CommunicationBusOrchestrationStep CreateCommunicationBusStep(
        AIOrchestrationGraph graph,
        AIOrchestrationNode node,
        Dictionary<string, MsAIAgent> agentLookup)
    {
        var config = node.Config as AIOrchestrationCommunicationBusNodeConfig;

        // Find all agent nodes connected TO this bus
        var incomingAgentNodeIds = graph.Edges
            .Where(e => e.TargetNodeId == node.Id)
            .Select(e => e.SourceNodeId)
            .ToList();

        var participants = new List<MsAIAgent>();
        MsAIAgent? manager = null;

        foreach (var agentNodeId in incomingAgentNodeIds)
        {
            var agentNode = graph.Nodes.FirstOrDefault(n => n.Id == agentNodeId);
            if (agentNode is null || !agentLookup.TryGetValue(agentNodeId, out var mafAgent))
            {
                continue;
            }

            var agentConfig = agentNode.Config as AIOrchestrationAgentNodeConfig;
            if (agentConfig?.IsManager == true)
            {
                manager = mafAgent;
            }
            else
            {
                participants.Add(mafAgent);
            }
        }

        return new CommunicationBusOrchestrationStep(
            node.Id,
            node.Label,
            participants,
            manager,
            config?.MaxIterations ?? 40,
            config?.TerminationMessage);
    }

    /// <summary>
    /// Gets the successor nodes for a given node ID by following outgoing edges.
    /// </summary>
    private static List<AIOrchestrationNode> GetSuccessors(AIOrchestrationGraph graph, string nodeId)
    {
        var outgoingEdges = graph.Edges
            .Where(e => e.SourceNodeId == nodeId)
            .OrderBy(e => e.Priority ?? int.MaxValue);

        return outgoingEdges
            .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.TargetNodeId))
            .Where(n => n is not null)
            .ToList()!;
    }
}
