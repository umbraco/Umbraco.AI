using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Builder for the test feature collection.
/// Enables registration and discovery of test features via the [AiTestFeature] attribute.
/// </summary>
public class AiTestFeatureCollectionBuilder
    : LazyCollectionBuilderBase<AiTestFeatureCollectionBuilder, AiTestFeatureCollection, IAiTestFeature>
{
    /// <inheritdoc />
    protected override AiTestFeatureCollectionBuilder This => this;
}
