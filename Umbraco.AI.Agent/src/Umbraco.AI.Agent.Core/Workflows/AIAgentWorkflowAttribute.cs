namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// Attribute to mark AI agent workflow implementations for auto-discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="IAIAgentWorkflow"/>
/// to enable automatic discovery and registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentWorkflow("sequential-pipeline", "Sequential Pipeline")]
/// public class SequentialPipelineWorkflow : AIAgentWorkflowBase { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIAgentWorkflowAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this workflow.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name for this workflow.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets an optional description of what this workflow does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentWorkflowAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow.</param>
    /// <param name="name">The display name for the workflow.</param>
    public AIAgentWorkflowAttribute(string id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
    }
}
