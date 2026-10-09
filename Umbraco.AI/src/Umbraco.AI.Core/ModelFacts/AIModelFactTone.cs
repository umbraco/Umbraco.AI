namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// How a <see cref="AIModelFact"/> should be emphasized when displayed.
/// </summary>
public enum AIModelFactTone
{
    /// <summary>
    /// No special emphasis.
    /// </summary>
    Neutral,

    /// <summary>
    /// A favorable fact.
    /// </summary>
    Positive,

    /// <summary>
    /// A fact the user should notice, such as a deprecation. Warnings are listed first.
    /// </summary>
    Warning
}
