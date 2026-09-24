using System.Text.Json.Serialization;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// Wire shapes for Jev's real <c>/v1/systemone</c> API, confirmed against
/// <see href="https://docs.typesafe.ai/api"/> during T11's live verification (2026-09-24) — these
/// are no longer placeholders. The endpoint answers a batch of named questions per call; the spike
/// always sends exactly one, keyed <c>"q"</c>.
/// </summary>
public sealed record JevSystemOneRequest(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("questions")] Dictionary<string, JevQuestionDto> Questions);

public sealed record JevQuestionDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("instructions")] string Instructions,
    [property: JsonPropertyName("criteria"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Criteria = null);

public sealed record JevSystemOneResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("answers")] Dictionary<string, JevAnswerDto> Answers);

/// <summary>
/// One answer from Jev. Only <see cref="Type"/> plus the field matching it are populated —
/// <c>noul</c> is itself a 0.0-1.0 probability of "yes" (not a boolean), with no separate
/// <c>confidence</c> field; <c>choice</c>/<c>score</c> each carry their own <c>confidence</c>.
/// </summary>
public sealed record JevAnswerDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("noul")] double? Noul,
    [property: JsonPropertyName("choice")] string? Choice,
    [property: JsonPropertyName("score")] double? Score,
    [property: JsonPropertyName("confidence")] double? Confidence);
