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

    /// <summary>
    /// The content to judge against <see cref="Instructions"/>, when there is any.
    /// </summary>
    public string? Context { get; init; }
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
    /// The score's labels, lowest first. Must contain between 2 and 10 non-blank entries.
    /// </summary>
    public required IReadOnlyList<string> Levels { get; init; }
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
