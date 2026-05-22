using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// An ordered collection builder for <see cref="IAIProviderErrorClassifier"/> implementations.
/// </summary>
/// <remarks>
/// Provider packages contribute their own classifier and insert it before the built-in
/// fallbacks so it gets first shot at the exception:
/// <code>
/// builder.AIProviderErrorClassifiers()
///     .InsertBefore&lt;ClientModelProviderErrorClassifier, AnthropicProviderErrorClassifier&gt;();
/// </code>
/// The first classifier returning a non-null <see cref="AIProviderErrorInfo"/> wins.
/// </remarks>
public class AIProviderErrorClassifierCollectionBuilder
    : OrderedCollectionBuilderBase<AIProviderErrorClassifierCollectionBuilder, AIProviderErrorClassifierCollection, IAIProviderErrorClassifier>
{
    /// <inheritdoc />
    protected override AIProviderErrorClassifierCollectionBuilder This => this;
}
