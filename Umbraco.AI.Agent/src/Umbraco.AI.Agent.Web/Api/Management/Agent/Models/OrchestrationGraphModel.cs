namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for the orchestration workflow graph.
/// </summary>
public class OrchestrationGraphModel
{
    /// <summary>
    /// The nodes in the graph (agents, functions, routers, etc.).
    /// </summary>
    public IList<OrchestrationNodeModel> Nodes { get; set; } = [];

    /// <summary>
    /// The edges connecting nodes in the graph.
    /// </summary>
    public IList<OrchestrationEdgeModel> Edges { get; set; } = [];
}
