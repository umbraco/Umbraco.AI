using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Output produced by the <see cref="AskChoiceDecisionAction"/>.
/// </summary>
public sealed class AskChoiceDecisionOutput
{
    /// <summary>
    /// Gets the selected option's key.
    /// </summary>
    [Field(Label = "Choice", Description = "The selected option's key.")]
    public string Choice { get; init; } = string.Empty;

    /// <summary>
    /// Gets the provider's own confidence summary for <see cref="Choice"/>, from 0.0 to 1.0, when
    /// reported. Empty when the provider doesn't report one.
    /// </summary>
    [Field(Label = "Confidence",
        Description = "The provider's own confidence summary for the choice, from 0.0 to 1.0, when reported.",
        SortOrder = 1)]
    public double? Confidence { get; init; }
}
