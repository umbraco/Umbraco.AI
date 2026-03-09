using System.Reflection;
using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// Base class for AI agent workflow implementations without settings.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply the <see cref="AIAgentWorkflowAttribute"/> to define a workflow.
/// The base class reads metadata from the attribute automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentWorkflow("sequential-pipeline", "Sequential Pipeline")]
/// public class SequentialPipelineWorkflow : AIAgentWorkflowBase
/// {
///     public override Task&lt;Workflow&gt; BuildWorkflowAsync(AIAgent agent, JsonElement? settings, CancellationToken ct)
///     {
///         // Build your workflow here
///     }
/// }
/// </code>
/// </example>
public abstract class AIAgentWorkflowBase : IAIAgentWorkflow
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public virtual Type? SettingsType => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentWorkflowBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class is missing the <see cref="AIAgentWorkflowAttribute"/>.
    /// </exception>
    protected AIAgentWorkflowBase()
    {
        var attribute = GetType().GetCustomAttribute<AIAgentWorkflowAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The AI agent workflow '{GetType().FullName}' is missing the required {nameof(AIAgentWorkflowAttribute)}.");

        Id = attribute.Id;
        Name = attribute.Name;
        Description = attribute.Description;
    }

    /// <inheritdoc />
    public virtual AIEditableModelSchema? GetSettingsSchema() => null;

    /// <inheritdoc />
    public abstract Task<Workflow> BuildWorkflowAsync(AIAgent agent, JsonElement? settings, CancellationToken cancellationToken);
}
