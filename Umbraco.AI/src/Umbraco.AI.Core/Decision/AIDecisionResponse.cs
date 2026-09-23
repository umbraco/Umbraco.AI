using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed answer returned by an <see cref="IAIDecisionClient"/>. One flat shape covers all three
/// <see cref="AIDecisionKind"/> values — only the field matching <see cref="Kind"/> is set.
/// </summary>
/// <remarks>
/// <see cref="ForBinary"/>, <see cref="ForChoice"/>, and <see cref="ForScore"/> are the intended
/// construction path — they keep <see cref="Kind"/> in sync with the field that was actually set.
/// The object initializer is still public, so callers/providers can bypass them if they need to.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionResponse
{
    /// <summary>The shape of answer this response carries.</summary>
    public required AIDecisionKind Kind { get; init; }

    /// <summary>Set iff <see cref="Kind"/> is <see cref="AIDecisionKind.Binary"/>.</summary>
    public bool? BinaryAnswer { get; init; }

    /// <summary>Set iff <see cref="Kind"/> is <see cref="AIDecisionKind.Choice"/>.</summary>
    public string? SelectedChoice { get; init; }

    /// <summary>Set iff <see cref="Kind"/> is <see cref="AIDecisionKind.Score"/>.</summary>
    public double? Score { get; init; }

    /// <summary>The model's confidence in this answer, from 0.0 to 1.0. Always present.</summary>
    public required double Confidence { get; init; }

    /// <summary>The model that produced this answer, when known.</summary>
    public string? ModelId { get; init; }

    /// <summary>Token/usage counts for this call, when reported by the provider.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>Builds a correctly-shaped <see cref="AIDecisionKind.Binary"/> response.</summary>
    /// <param name="answer">The yes/no answer.</param>
    /// <param name="confidence">The model's confidence in the answer, from 0.0 to 1.0.</param>
    /// <param name="modelId">The model that produced this answer, when known.</param>
    /// <param name="usage">Token/usage counts for this call, when reported by the provider.</param>
    public static AIDecisionResponse ForBinary(
        bool answer,
        double confidence,
        string? modelId = null,
        UsageDetails? usage = null) => new()
    {
        Kind = AIDecisionKind.Binary,
        BinaryAnswer = answer,
        Confidence = confidence,
        ModelId = modelId,
        Usage = usage,
    };

    /// <summary>Builds a correctly-shaped <see cref="AIDecisionKind.Choice"/> response.</summary>
    /// <param name="choice">The selected choice.</param>
    /// <param name="confidence">The model's confidence in the answer, from 0.0 to 1.0.</param>
    /// <param name="modelId">The model that produced this answer, when known.</param>
    /// <param name="usage">Token/usage counts for this call, when reported by the provider.</param>
    public static AIDecisionResponse ForChoice(
        string choice,
        double confidence,
        string? modelId = null,
        UsageDetails? usage = null) => new()
    {
        Kind = AIDecisionKind.Choice,
        SelectedChoice = choice,
        Confidence = confidence,
        ModelId = modelId,
        Usage = usage,
    };

    /// <summary>Builds a correctly-shaped <see cref="AIDecisionKind.Score"/> response.</summary>
    /// <param name="score">The numeric score.</param>
    /// <param name="confidence">The model's confidence in the answer, from 0.0 to 1.0.</param>
    /// <param name="modelId">The model that produced this answer, when known.</param>
    /// <param name="usage">Token/usage counts for this call, when reported by the provider.</param>
    public static AIDecisionResponse ForScore(
        double score,
        double confidence,
        string? modelId = null,
        UsageDetails? usage = null) => new()
    {
        Kind = AIDecisionKind.Score,
        Score = score,
        Confidence = confidence,
        ModelId = modelId,
        Usage = usage,
    };
}
