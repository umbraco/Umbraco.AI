using System.Text.Json.Serialization;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// The wire shape of a Jev answer. Field names are placeholders pending confirmation against Jev's
/// real API docs (see <see cref="JevSpikeDecisionClient"/>'s remarks) — covers all three of Jev's
/// answer kinds (<c>noul</c>/<c>choice</c>/<c>score</c>) in one flat DTO, mirroring
/// <c>AIDecisionResponse</c>'s own flat-with-discriminant shape.
/// </summary>
public sealed record JevAnswerDto(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("noul")] bool? Noul,
    [property: JsonPropertyName("choice")] string? Choice,
    [property: JsonPropertyName("score")] double? Score,
    [property: JsonPropertyName("confidence")] double Confidence);
