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
    /// Gets the model's confidence in <see cref="Choice"/>, from 0.0 to 1.0.
    /// </summary>
    [Field(Label = "Confidence",
        Description = "The model's confidence in the choice, from 0.0 to 1.0.",
        SortOrder = 1)]
    public double Confidence { get; init; }
}
