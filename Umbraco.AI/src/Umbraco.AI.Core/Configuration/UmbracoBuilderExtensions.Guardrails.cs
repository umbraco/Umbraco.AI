using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI guardrail collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI guardrail evaluator collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI guardrail evaluator collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude evaluators from the collection. Example:
    /// <code>
    /// builder.AIGuardrailEvaluators()
    ///     .Add&lt;MyCustomEvaluator&gt;()
    ///     .Exclude&lt;SomeUnwantedEvaluator&gt;();
    /// </code>
    /// </remarks>
    public static AIGuardrailEvaluatorCollectionBuilder AIGuardrailEvaluators(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIGuardrailEvaluatorCollectionBuilder>();

    /// <summary>
    /// Gets the AI guardrail resolver collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI guardrail resolver collection builder.</returns>
    /// <remarks>
    /// Use this to configure the order of guardrail resolution. Example:
    /// <code>
    /// builder.AIGuardrailResolvers()
    ///     .Append&lt;ProfileGuardrailResolver&gt;()
    ///     .InsertAfter&lt;ProfileGuardrailResolver, AgentGuardrailResolver&gt;();
    /// </code>
    /// </remarks>
    public static AIGuardrailResolverCollectionBuilder AIGuardrailResolvers(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIGuardrailResolverCollectionBuilder>();
}
