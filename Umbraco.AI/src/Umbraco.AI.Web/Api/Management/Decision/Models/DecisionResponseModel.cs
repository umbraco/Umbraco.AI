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
    /// The model's estimated probability that the answer is "yes", from 0.0 to 1.0. The probability
    /// itself is the distribution — there is no separate confidence for a binary answer.
    /// </summary>
    public required double TrueProbability { get; init; }
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
    /// The provider's summary confidence in <see cref="Choice"/>, from 0.0 to 1.0, when reported. Not
    /// comparable across providers. Omitted entirely when the provider gives none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Confidence { get; init; }

    /// <summary>
    /// The model's estimated probability for every option, keyed by option key.
    /// </summary>
    public required IReadOnlyDictionary<string, double> Probabilities { get; init; }
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
    /// The provider's summary confidence in <see cref="Score"/>, from 0.0 to 1.0, when reported. Not
    /// comparable across providers. Omitted entirely when the provider gives none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Confidence { get; init; }

    /// <summary>
    /// The model's estimated probability for every level, keyed by its 0-based index
    /// (<c>"0"</c>..<c>"N-1"</c>).
    /// </summary>
    public required IReadOnlyDictionary<int, double> Probabilities { get; init; }
}
