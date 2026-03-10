using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// A collection of AI agent workflows.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides lookup methods to find workflows by ID.
/// Workflows are auto-discovered via the <see cref="AIAgentWorkflowAttribute"/>.
/// </para>
/// </remarks>
public sealed class AIAgentWorkflowCollection : BuilderCollectionBase<IAIAgentWorkflow>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentWorkflowCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the workflows.</param>
    public AIAgentWorkflowCollection(Func<IEnumerable<IAIAgentWorkflow>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a workflow by its unique identifier.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <returns>The workflow, or <c>null</c> if not found.</returns>
    public IAIAgentWorkflow? GetById(string workflowId)
        => this.FirstOrDefault(w => w.Id.Equals(workflowId, StringComparison.OrdinalIgnoreCase));
}
