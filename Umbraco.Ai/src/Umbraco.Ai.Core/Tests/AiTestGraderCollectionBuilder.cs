using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Builder for the test grader collection.
/// Enables registration and discovery of graders via the [AiTestGrader] attribute.
/// </summary>
public class AiTestGraderCollectionBuilder
    : LazyCollectionBuilderBase<AiTestGraderCollectionBuilder, AiTestGraderCollection, IAiTestGrader>
{
    /// <inheritdoc />
    protected override AiTestGraderCollectionBuilder This => this;
}
