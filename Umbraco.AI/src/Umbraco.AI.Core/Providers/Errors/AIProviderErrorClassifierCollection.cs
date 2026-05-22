using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// A collection of <see cref="IAIProviderErrorClassifier"/> instances consulted in order to
/// classify an exception. The first classifier returning a non-null result wins.
/// </summary>
/// <remarks>
/// Order is controlled by <see cref="AIProviderErrorClassifierCollectionBuilder"/> via
/// <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c>. Provider-specific classifiers
/// should be inserted before the built-in fallbacks so they take precedence.
/// </remarks>
public sealed class AIProviderErrorClassifierCollection : BuilderCollectionBase<IAIProviderErrorClassifier>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="AIProviderErrorClassifierCollection"/> class.
    /// </summary>
    public AIProviderErrorClassifierCollection(Func<IEnumerable<IAIProviderErrorClassifier>> items)
        : base(items)
    { }
}
