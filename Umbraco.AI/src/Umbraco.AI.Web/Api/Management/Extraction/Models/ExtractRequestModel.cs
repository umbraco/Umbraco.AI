using System.Text.Json.Serialization;

namespace Umbraco.AI.Web.Api.Management.Extraction.Models;

/// <summary>
/// Request model for extracting values from text.
/// </summary>
public class ExtractRequestModel
{
    /// <summary>
    /// Optional profile ID or alias to use. If omitted, the default Extraction profile is used.
    /// </summary>
    public string? ProfileIdOrAlias { get; init; }

    /// <summary>
    /// The text to extract values from.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// The date that relative expressions in the text, such as "yesterday", are resolved against
    /// (<c>yyyy-MM-dd</c>). When omitted, the provider decides.
    /// </summary>
    public DateOnly? ReferenceDate { get; init; }

    /// <summary>
    /// The fields to extract. Each field's <c>id</c> must be unique and is the key of its answer.
    /// </summary>
    public required IReadOnlyList<ExtractionFieldModel> Fields { get; init; }
}

/// <summary>
/// Base class for a polymorphic extraction field, discriminated by <c>$type</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextExtractionFieldModel), "text")]
[JsonDerivedType(typeof(NumberExtractionFieldModel), "number")]
[JsonDerivedType(typeof(DateExtractionFieldModel), "date")]
[JsonDerivedType(typeof(TimeExtractionFieldModel), "time")]
[JsonDerivedType(typeof(DateTimeExtractionFieldModel), "datetime")]
[JsonDerivedType(typeof(DurationExtractionFieldModel), "duration")]
[JsonDerivedType(typeof(ListExtractionFieldModel), "list")]
public abstract class ExtractionFieldModel
{
    /// <summary>
    /// The field's identifier, returned exactly as sent as the key of its answer. Must start with a
    /// letter or underscore, use only letters, digits and underscores, and be at most 64 characters.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// What to extract, as natural-language instructions given to the model.
    /// </summary>
    public required string Instructions { get; init; }
}

/// <summary>
/// A text field.
/// </summary>
public sealed class TextExtractionFieldModel : ExtractionFieldModel
{
}

/// <summary>
/// A number field.
/// </summary>
public sealed class NumberExtractionFieldModel : ExtractionFieldModel
{
    /// <summary>
    /// The number of decimal places to round to, from 0 to 6.
    /// </summary>
    public int? Decimals { get; init; }

    /// <summary>
    /// Whether the value must be a whole number.
    /// </summary>
    public bool Integer { get; init; }

    /// <summary>
    /// The unit the value is expressed in, such as <c>DKK</c>.
    /// </summary>
    public string? Unit { get; init; }
}

/// <summary>
/// A date field.
/// </summary>
public sealed class DateExtractionFieldModel : ExtractionFieldModel
{
}

/// <summary>
/// A time-of-day field.
/// </summary>
public sealed class TimeExtractionFieldModel : ExtractionFieldModel
{
}

/// <summary>
/// A date-and-time field.
/// </summary>
public sealed class DateTimeExtractionFieldModel : ExtractionFieldModel
{
}

/// <summary>
/// A duration field.
/// </summary>
public sealed class DurationExtractionFieldModel : ExtractionFieldModel
{
    /// <summary>
    /// The unit the value is expressed in: <c>seconds</c>, <c>minutes</c>, <c>hours</c>, <c>days</c>,
    /// <c>weeks</c>, <c>months</c> or <c>years</c>.
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// The number of decimal places to round to, from 0 to 6.
    /// </summary>
    public int? Decimals { get; init; }

    /// <summary>
    /// Whether the value must be a whole number.
    /// </summary>
    public bool Integer { get; init; }
}

/// <summary>
/// A list field.
/// </summary>
public sealed class ListExtractionFieldModel : ExtractionFieldModel
{
    /// <summary>
    /// The type of each item: <c>text</c> (the default), <c>number</c> or <c>date</c>.
    /// </summary>
    public string? ItemType { get; init; }
}
