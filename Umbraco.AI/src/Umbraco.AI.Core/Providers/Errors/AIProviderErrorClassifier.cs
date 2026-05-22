namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// The public-facing classifier — runs the registered <see cref="IAIProviderErrorClassifier"/>
/// implementations in collection order and guarantees a non-null result.
/// </summary>
/// <remarks>
/// <para>
/// Order is controlled via <see cref="AIProviderErrorClassifierCollectionBuilder"/>; the first
/// classifier returning a non-null <see cref="AIProviderErrorInfo"/> wins. Anything still
/// unclassified becomes <see cref="AIProviderErrorCategory.Unknown"/>.
/// </para>
/// <para>
/// Resolve this type (not <see cref="IAIProviderErrorClassifier"/>) when you need to classify
/// an exception — it always returns a populated <see cref="AIProviderErrorInfo"/>.
/// </para>
/// </remarks>
public sealed class AIProviderErrorClassifier
{
    private readonly AIProviderErrorClassifierCollection _classifiers;

    /// <summary>
    /// Initialises a new instance with the registered classifier collection.
    /// </summary>
    public AIProviderErrorClassifier(AIProviderErrorClassifierCollection classifiers)
    {
        _classifiers = classifiers;
    }

    /// <summary>
    /// Classifies the given exception, falling back to <see cref="AIProviderErrorCategory.Unknown"/>
    /// if no registered classifier recognises it.
    /// </summary>
    public AIProviderErrorInfo Classify(Exception exception)
    {
        foreach (var classifier in _classifiers)
        {
            var info = classifier.Classify(exception);
            if (info is not null)
            {
                return info;
            }
        }

        return new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            "An unexpected error occurred. Please try again.",
            ProviderCode: null,
            exception.Message);
    }
}
