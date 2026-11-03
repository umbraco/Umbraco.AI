using System.Text.Json.Serialization;
using Umbraco.AI.Web.Api.Common.Json;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Extraction.Models;

/// <summary>
/// Response model for an extraction: one answer per requested field.
/// </summary>
public class ExtractResponseModel
{
    /// <summary>
    /// The answers, keyed by field id. Keys are written exactly as the caller sent them, not
    /// camel-cased like other dictionary keys in this API.
    /// </summary>
    [JsonConverter(typeof(ExactKeyDictionaryJsonConverter<ExtractionAnswerModel>))]
    public required IReadOnlyDictionary<string, ExtractionAnswerModel> Answers { get; init; }

    /// <summary>
    /// The model that produced the answers, when known.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Token usage for the call, when reported by the provider.
    /// </summary>
    public UsageModel? Usage { get; init; }
}

/// <summary>
/// A character range in the request's <c>state</c>, start inclusive and end exclusive.
/// </summary>
public class ExtractionSpanModel
{
    /// <summary>
    /// The start offset.
    /// </summary>
    public required int Start { get; init; }

    /// <summary>
    /// The end offset.
    /// </summary>
    public required int End { get; init; }
}

/// <summary>
/// Base class for a polymorphic extraction answer, discriminated by <c>$type</c> to match its
/// field's <c>$type</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextExtractionAnswerModel), "text")]
[JsonDerivedType(typeof(NumberExtractionAnswerModel), "number")]
[JsonDerivedType(typeof(DateExtractionAnswerModel), "date")]
[JsonDerivedType(typeof(TimeExtractionAnswerModel), "time")]
[JsonDerivedType(typeof(DateTimeExtractionAnswerModel), "datetime")]
[JsonDerivedType(typeof(DurationExtractionAnswerModel), "duration")]
[JsonDerivedType(typeof(ListExtractionAnswerModel), "list")]
public abstract class ExtractionAnswerModel
{
    /// <summary>
    /// The provider's confidence in the answer, from 0.0 to 1.0, when reported. Not comparable across
    /// providers. Omitted when the provider gives none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Confidence { get; init; }

    /// <summary>
    /// The provider's probability that the value is absent from the text, from 0.0 to 1.0, when
    /// reported. Omitted when the provider gives none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? NullProbability { get; init; }

    /// <summary>
    /// Whether a value was found in the text.
    /// </summary>
    public required bool IsFound { get; init; }
}

/// <summary>
/// The answer to a text field.
/// </summary>
public sealed class TextExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted text, or null when not found.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Where the value sits in the text, when the provider reports it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExtractionSpanModel? Span { get; init; }
}

/// <summary>
/// The answer to a number field.
/// </summary>
public sealed class NumberExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted number, or null when not found.
    /// </summary>
    public decimal? Value { get; init; }
}

/// <summary>
/// The answer to a date field.
/// </summary>
public sealed class DateExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted date (<c>yyyy-MM-dd</c>), or null when not found.
    /// </summary>
    public DateOnly? Value { get; init; }
}

/// <summary>
/// The answer to a time-of-day field.
/// </summary>
public sealed class TimeExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted time of day, or null when not found.
    /// </summary>
    public TimeOnly? Value { get; init; }
}

/// <summary>
/// The answer to a date-and-time field.
/// </summary>
public sealed class DateTimeExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted date and time, or null when not found.
    /// </summary>
    public DateTime? Value { get; init; }
}

/// <summary>
/// The answer to a duration field.
/// </summary>
public sealed class DurationExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted length in <see cref="Unit"/>, or null when not found.
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>
    /// The unit <see cref="Value"/> is expressed in.
    /// </summary>
    public required string Unit { get; init; }
}

/// <summary>
/// The answer to a list field.
/// </summary>
public sealed class ListExtractionAnswerModel : ExtractionAnswerModel
{
    /// <summary>
    /// The extracted items, in text order.
    /// </summary>
    public required IReadOnlyList<string> Values { get; init; }

    /// <summary>
    /// Where each item sits in the text, when the provider reports it.
    /// </summary>
    public required IReadOnlyList<ExtractionSpanModel> Spans { get; init; }
}
