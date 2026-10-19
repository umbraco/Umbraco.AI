using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Output produced by the <see cref="AskScoreDecisionAction"/>.
/// </summary>
public sealed class AskScoreDecisionOutput
{
    /// <summary>
    /// Gets the 0-based, fractional index into the configured levels the model landed on.
    /// </summary>
    [Field(Label = "Score", Description = "The 0-based, fractional index into the configured levels.")]
    public double Score { get; init; }

    /// <summary>
    /// Gets the label of the level nearest to <see cref="Score"/>.
    /// </summary>
    [Field(Label = "Level", Description = "The label of the level nearest to the score.", SortOrder = 1)]
    public string Level { get; init; } = string.Empty;

    /// <summary>
    /// Gets the provider's own confidence summary for <see cref="Score"/>, from 0.0 to 1.0, when
    /// reported. Empty when the provider doesn't report one.
    /// </summary>
    [Field(Label = "Confidence",
        Description = "The provider's own confidence summary for the score, from 0.0 to 1.0, when reported.",
        SortOrder = 2)]
    public double? Confidence { get; init; }
}
