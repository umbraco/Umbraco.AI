using System.Text.Json.Serialization;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Decision.Models;

/// <summary>
/// Base class for a polymorphic Decision response, discriminated by <c>$type</c> to match the
/// question's own <c>$type</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BinaryDecisionResponseModel), "binary")]
[JsonDerivedType(typeof(ChoiceDecisionResponseModel), "choice")]
[JsonDerivedType(typeof(ScoreDecisionResponseModel), "score")]
public abstract class DecisionResponseModel
{
    /// <summary>
    /// The model that produced this answer, when known.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Token usage for this call, when reported by the provider.
    /// </summary>
    public UsageModel? Usage { get; init; }
}

/// <summary>
/// The answer to a binary (yes/no) Decision question.
/// </summary>
public sealed class BinaryDecisionResponseModel : DecisionResponseModel
{
    /// <summary>
    /// The yes/no answer.
    /// </summary>
    public required bool Answer { get; init; }

    /// <summary>
    /// The model's estimated probability that the answer is "yes", from 0.0 to 1.0.
    /// </summary>
    public required double Probability { get; init; }

    /// <summary>
    /// The model's confidence in <see cref="Answer"/>, from 0.0 to 1.0.
    /// </summary>
    public required double Confidence { get; init; }
}

/// <summary>
/// The answer to a choice Decision question.
/// </summary>
public sealed class ChoiceDecisionResponseModel : DecisionResponseModel
{
    /// <summary>
    /// The selected option's key.
    /// </summary>
    public required string Choice { get; init; }

    /// <summary>
    /// The model's confidence in <see cref="Choice"/>, from 0.0 to 1.0.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// The model's estimated probability for each option, keyed by option key, when reported.
    /// </summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();
}

/// <summary>
/// The answer to a score Decision question.
/// </summary>
public sealed class ScoreDecisionResponseModel : DecisionResponseModel
{
    /// <summary>
    /// The 0-based, fractional index into the question's levels the model landed on.
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// The label of the level nearest to <see cref="Score"/>.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// The model's confidence in <see cref="Score"/>, from 0.0 to 1.0.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// The model's estimated probability for each level, keyed by label, when reported.
    /// </summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();
}
