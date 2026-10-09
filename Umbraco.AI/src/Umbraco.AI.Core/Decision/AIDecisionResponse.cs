using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// The answer to an <see cref="AIDecisionRequest"/>: one typed <see cref="AIDecisionAnswer"/> per
/// question, keyed by the question's <see cref="AIDecisionQuestion.Id"/>, plus the model and usage for
/// the whole call.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public class AIDecisionResponse
{
    /// <summary>The answer to each question, keyed by <see cref="AIDecisionQuestion.Id"/>.</summary>
    public required IReadOnlyDictionary<string, AIDecisionAnswer> Answers { get; init; }

    /// <summary>The concrete model that answered, e.g. <c>"jev-1.13.0"</c>, when known.</summary>
    public string? ModelId { get; init; }

    /// <summary>Token/usage counts for the whole call, when reported by the provider.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The provider's own, unmapped representation of this response, when it chooses to expose one.</summary>
    public object? RawRepresentation { get; init; }
}

/// <summary>
/// An <see cref="AIDecisionResponse"/> to a single-question request, with <see cref="Answer"/> typed to
/// that question's answer shape so a caller using <c>IAIDecisionService.AskAsync</c> gets no cast.
/// </summary>
/// <typeparam name="TAnswer">The concrete <see cref="AIDecisionAnswer"/> the question yields.</typeparam>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionResponse<TAnswer> : AIDecisionResponse
    where TAnswer : AIDecisionAnswer
{
    /// <summary>The single answer, typed. Also present (as the base type) in <see cref="AIDecisionResponse.Answers"/>.</summary>
    public required TAnswer Answer { get; init; }
}
