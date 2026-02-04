using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI testing collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI test feature collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI test feature collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude test features from the collection. Example:
    /// <code>
    /// builder.AITestFeatures()
    ///     .Add&lt;MyCustomTestFeature&gt;()
    ///     .Exclude&lt;SomeUnwantedFeature&gt;();
    /// </code>
    /// </remarks>
    public static AITestFeatureCollectionBuilder AITestFeatures(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AITestFeatureCollectionBuilder>();

    /// <summary>
    /// Gets the AI test grader collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI test grader collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude graders from the collection. Example:
    /// <code>
    /// builder.AITestGraders()
    ///     .Add&lt;MyCustomGrader&gt;()
    ///     .Exclude&lt;SomeUnwantedGrader&gt;();
    /// </code>
    /// </remarks>
    public static AITestGraderCollectionBuilder AITestGraders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AITestGraderCollectionBuilder>();
}
