using System.Text.Json.Serialization;

namespace Umbraco.AI.Web.Api.Management.Decision.Models;

/// <summary>
/// Request model for asking a Decision question.
/// </summary>
public class AskDecisionRequestModel
{
    /// <summary>
    /// Optional profile ID or alias to use. If omitted, the default Decision profile is used.
    /// </summary>
    public string? ProfileIdOrAlias { get; init; }

    /// <summary>
    /// The content being judged by <see cref="Question"/>, when there is any. Shared state, mirroring
    /// <c>Umbraco.AI.Core.Decision.AIDecisionRequest.State</c>.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// The question to ask.
    /// </summary>
    public required DecisionQuestionModel Question { get; init; }
}

/// <summary>
/// Base class for a polymorphic Decision question, discriminated by <c>$type</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BinaryDecisionQuestionModel), "binary")]
[JsonDerivedType(typeof(ChoiceDecisionQuestionModel), "choice")]
[JsonDerivedType(typeof(ScoreDecisionQuestionModel), "score")]
public abstract class DecisionQuestionModel
{
    /// <summary>
    /// What to decide — the natural-language instructions given to the model.
    /// </summary>
    public required string Instructions { get; init; }
}

/// <summary>
/// A yes/no Decision question.
/// </summary>
public sealed class BinaryDecisionQuestionModel : DecisionQuestionModel
{
    /// <summary>
    /// What "yes" means, when it needs spelling out beyond <see cref="DecisionQuestionModel.Instructions"/>.
    /// </summary>
    public string? TrueCriteria { get; init; }

    /// <summary>
    /// What "no" means, when it needs spelling out beyond <see cref="DecisionQuestionModel.Instructions"/>.
    /// </summary>
    public string? FalseCriteria { get; init; }
}

/// <summary>
/// A Decision question answered by selecting one of a fixed set of options.
/// </summary>
public sealed class ChoiceDecisionQuestionModel : DecisionQuestionModel
{
    /// <summary>
    /// The options to choose from. Must contain between 2 and 255 entries with unique, non-blank keys.
    /// </summary>
    public required IReadOnlyList<DecisionOptionModel> Options { get; init; }
}

/// <summary>
/// A Decision question answered with a numeric score against labelled levels.
/// </summary>
public sealed class ScoreDecisionQuestionModel : DecisionQuestionModel
{
    /// <summary>
    /// The score's levels, lowest first. Must contain between 2 and 10 entries with non-blank descriptions.
    /// </summary>
    public required IReadOnlyList<DecisionScoreLevelModel> Levels { get; init; }
}

/// <summary>
/// One selectable option of a <see cref="ChoiceDecisionQuestionModel"/>.
/// </summary>
public sealed class DecisionOptionModel
{
    /// <summary>
    /// The stable identifier returned as the choice response's <c>choice</c> value when selected.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// An optional, human-readable elaboration of what <see cref="Key"/> means.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// One labelled level of a <see cref="ScoreDecisionQuestionModel"/>, lowest first.
/// </summary>
public sealed class DecisionScoreLevelModel
{
    /// <summary>
    /// The level's label, e.g. <c>"poor"</c>, <c>"ok"</c>, <c>"good"</c>.
    /// </summary>
    public required string Description { get; init; }
}
