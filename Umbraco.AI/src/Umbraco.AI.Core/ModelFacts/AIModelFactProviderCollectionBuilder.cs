using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// An ordered collection builder for model fact providers.
/// </summary>
/// <remarks>
/// Use this builder to add providers or change their order:
/// <code>
/// builder.AIModelFactProviders()
///     .Append&lt;MyModelFactProvider&gt;();
/// </code>
/// </remarks>
public class AIModelFactProviderCollectionBuilder
    : OrderedCollectionBuilderBase<AIModelFactProviderCollectionBuilder, AIModelFactProviderCollection, IAIModelFactProvider>
{
    /// <inheritdoc />
    protected override AIModelFactProviderCollectionBuilder This => this;
}
