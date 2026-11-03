#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental extraction capability surface

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// The short, stable kind name of an <see cref="AIExtractionField"/> (<c>text</c>, <c>number</c>, ...),
/// shared by the tracking and telemetry middleware so the audit log and spans can't drift apart.
/// </summary>
internal static class ExtractionFieldKind
{
    internal static string Of(AIExtractionField field) => field switch
    {
        AITextExtractionField => "text",
        AINumberExtractionField => "number",
        AIDateExtractionField => "date",
        AITimeExtractionField => "time",
        AIDateTimeExtractionField => "datetime",
        AIDurationExtractionField => "duration",
        AIListExtractionField => "list",
        _ => field.GetType().Name,
    };

    internal static string Of(AIExtractionAnswer answer) => answer switch
    {
        AITextExtractionAnswer => "text",
        AINumberExtractionAnswer => "number",
        AIDateExtractionAnswer => "date",
        AITimeExtractionAnswer => "time",
        AIDateTimeExtractionAnswer => "datetime",
        AIDurationExtractionAnswer => "duration",
        AIListExtractionAnswer => "list",
        _ => answer.GetType().Name,
    };
}
