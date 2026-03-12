using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// A collection builder for AI agent workflows.
/// </summary>
/// <remarks>
/// <para>
/// Workflows are auto-discovered via <see cref="AIAgentWorkflowAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add workflows manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered workflows.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Composer
/// builder.AIAgentWorkflows()
///     .Add&lt;MyCustomWorkflow&gt;();
/// </code>
/// </example>
public class AIAgentWorkflowCollectionBuilder
    : LazyCollectionBuilderBase<AIAgentWorkflowCollectionBuilder, AIAgentWorkflowCollection, IAIAgentWorkflow>
{
    /// <inheritdoc />
    protected override AIAgentWorkflowCollectionBuilder This => this;
}
