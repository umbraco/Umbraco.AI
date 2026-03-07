using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Tools;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Builds a MAF workflow agent from a stored orchestration graph.
/// </summary>
/// <remarks>
/// <para>
/// Traverses the orchestration graph to create a MAF agent that:
/// <list type="bullet">
///   <item>Creates <see cref="ChatClientAgent"/> instances for Agent nodes.</item>
///   <item>Invokes registered AI tools for Function nodes.</item>
///   <item>Evaluates conditions for Router nodes.</item>
///   <item>Aggregates concurrent results for Aggregator nodes.</item>
///   <item>Delegates dynamically for Manager nodes.</item>
/// </list>
/// </para>
/// <para>
/// The graph is traversed starting from the single Start node, following edges
/// to build a sequential execution pipeline. Concurrent branches (fan-out) are
/// supported when a node has multiple outgoing edges.
/// </para>
/// </remarks>
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
        AIOrchestration orchestration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orchestration);

        var graph = orchestration.Graph;

        // Find start node
        var startNode = graph.Nodes.FirstOrDefault(n => n.Type == AIOrchestrationNodeType.Start)
            ?? throw new InvalidOperationException("Orchestration graph must have a Start node.");

        // Build agent lookup for the graph
        var agentLookup = await BuildAgentLookupAsync(graph, cancellationToken);

        // Build the execution pipeline starting from the Start node
        var pipeline = BuildPipeline(graph, startNode, agentLookup, orchestration);

        // Wrap the pipeline as a single composite agent
        return new OrchestrationPipelineAgent(
            orchestration.Name,
            orchestration.Description,
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
            if (node.Config.AgentId is null)
            {
                _logger.LogWarning("Agent node '{NodeId}' has no AgentId configured, skipping", node.Id);
                continue;
            }

            var agentDefinition = await _agentService.GetAgentAsync(node.Config.AgentId.Value, cancellationToken);
            if (agentDefinition is null)
            {
                _logger.LogWarning("Agent '{AgentId}' referenced by node '{NodeId}' not found, skipping", node.Config.AgentId, node.Id);
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
        Dictionary<string, MsAIAgent> agentLookup,
        AIOrchestration orchestration)
    {
        var steps = new List<OrchestrationStep>();
        var visited = new HashSet<string>();
        var queue = new Queue<AIOrchestrationNode>();

        // Get successors of Start node
        var startSuccessors = GetSuccessors(graph, startNode.Id);
        foreach (var successor in startSuccessors)
        {
            queue.Enqueue(successor);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.Id)) continue;

            var step = CreateStep(graph, node, agentLookup, orchestration);
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
        Dictionary<string, MsAIAgent> agentLookup,
        AIOrchestration orchestration)
    {
        return node.Type switch
        {
            AIOrchestrationNodeType.Agent => agentLookup.TryGetValue(node.Id, out var agent)
                ? new OrchestrationStep(node.Id, node.Label, OrchestrationStepType.Agent, Agent: agent)
                : null,

            AIOrchestrationNodeType.Function => !string.IsNullOrEmpty(node.Config.ToolName)
                ? new OrchestrationStep(node.Id, node.Label, OrchestrationStepType.Function, ToolName: node.Config.ToolName)
                : null,

            AIOrchestrationNodeType.Router => new OrchestrationStep(
                node.Id, node.Label, OrchestrationStepType.Router,
                Conditions: node.Config.Conditions,
                SuccessorEdges: graph.Edges.Where(e => e.SourceNodeId == node.Id).ToList()),

            AIOrchestrationNodeType.Aggregator => new OrchestrationStep(
                node.Id, node.Label, OrchestrationStepType.Aggregator,
                AggregationStrategy: node.Config.AggregationStrategy ?? AIOrchestrationAggregationStrategy.Concat),

            AIOrchestrationNodeType.Manager => new OrchestrationStep(
                node.Id, node.Label, OrchestrationStepType.Manager,
                ManagerInstructions: node.Config.ManagerInstructions),

            AIOrchestrationNodeType.End => new OrchestrationStep(node.Id, node.Label, OrchestrationStepType.End),

            _ => null,
        };
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
