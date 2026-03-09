using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI agent workflow collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI agent workflow collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI agent workflow collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude workflows from the collection. Example:
    /// <code>
    /// builder.AIAgentWorkflows()
    ///     .Add&lt;MyCustomWorkflow&gt;()
    ///     .Exclude&lt;SomeUnwantedWorkflow&gt;();
    /// </code>
    /// </remarks>
    public static AIAgentWorkflowCollectionBuilder AIAgentWorkflows(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIAgentWorkflowCollectionBuilder>();
}
