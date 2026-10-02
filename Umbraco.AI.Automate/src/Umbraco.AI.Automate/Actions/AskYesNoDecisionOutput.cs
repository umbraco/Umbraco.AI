using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Output produced by the <see cref="AskYesNoDecisionAction"/>.
/// </summary>
public sealed class AskYesNoDecisionOutput
{
    /// <summary>
    /// Gets the yes/no answer.
    /// </summary>
    [Field(Label = "Answer", Description = "The yes/no answer.")]
    public bool Answer { get; init; }

    /// <summary>
    /// Gets the model's estimated probability that the answer is "yes", from 0.0 to 1.0.
    /// </summary>
    [Field(Label = "Probability",
        Description = "The model's estimated probability that the answer is \"yes\", from 0.0 to 1.0.",
        SortOrder = 1)]
    public double Probability { get; init; }

    /// <summary>
    /// Gets the model's confidence in <see cref="Answer"/>, from 0.0 to 1.0.
    /// </summary>
    [Field(Label = "Confidence",
        Description = "The model's confidence in the answer, from 0.0 to 1.0.",
        SortOrder = 2)]
    public double Confidence { get; init; }
}
