using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// The answer to an <see cref="AIExtractionRequest"/>: one typed <see cref="AIExtractionAnswer"/> per
/// field, keyed by the field's <see cref="AIExtractionField.Id"/>, plus the model and usage for the
/// whole call.
/// </summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public class AIExtractionResponse
{
    /// <summary>The answer to each field, keyed by <see cref="AIExtractionField.Id"/>.</summary>
    public required IReadOnlyDictionary<string, AIExtractionAnswer> Answers { get; init; }

    /// <summary>The concrete model that answered, e.g. <c>"@cf/cloudflare/clairic"</c>, when known.</summary>
    public string? ModelId { get; init; }

    /// <summary>Token/usage counts for the whole call, when reported by the provider.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The provider's own, unmapped representation of this response, when it chooses to expose one.</summary>
    public object? RawRepresentation { get; init; }
}

/// <summary>
/// An <see cref="AIExtractionResponse"/> to a single-field request, with <see cref="Answer"/> typed to
/// that field's answer shape so a caller using <c>IAIExtractionService.ExtractAsync</c> gets no cast.
/// </summary>
/// <typeparam name="TAnswer">The concrete <see cref="AIExtractionAnswer"/> the field yields.</typeparam>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionResponse<TAnswer> : AIExtractionResponse
    where TAnswer : AIExtractionAnswer
{
    /// <summary>The single answer, typed. Also present (as the base type) in <see cref="AIExtractionResponse.Answers"/>.</summary>
    public required TAnswer Answer { get; init; }
}
