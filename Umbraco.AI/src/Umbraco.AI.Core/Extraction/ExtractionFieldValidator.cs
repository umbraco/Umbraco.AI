using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Structural validation for an <see cref="AIExtractionRequest"/>, shared between
/// <see cref="ValidatingExtractionClient"/> (the last line of defence for any C# caller) and the
/// Management API's extract controller (front-line validation that must run before profile resolution
/// and any provider call). A single set of rules means the two call sites can never drift.
/// </summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
internal static partial class ExtractionFieldValidator
{
    private const int MinDecimals = 0;
    private const int MaxDecimals = 6;

    // \z, not $: $ also matches before a trailing newline, which would let "orderId\n" through.
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,63}\z", RegexOptions.CultureInvariant)]
    private static partial Regex FieldIdPattern();

    /// <summary>
    /// Whether <paramref name="id"/> is a legal field id: a letter or underscore, then letters, digits
    /// or underscores, at most 64 characters.
    /// </summary>
    /// <remarks>Used by Umbraco.AI.Automate via InternalsVisibleTo, so renaming or changing it breaks an installed Automate at runtime.</remarks>
    /// <param name="id">The id to check.</param>
    /// <returns><see langword="true"/> when the id is legal.</returns>
    public static bool IsValidFieldId(string id) => FieldIdPattern().IsMatch(id);

    /// <summary>
    /// Validates <paramref name="request"/>'s batch shape and every one of its fields.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>
    /// A description of the first rule <paramref name="request"/> breaks, or <see langword="null"/>
    /// when it's valid.
    /// </returns>
    public static string? ValidateRequest(AIExtractionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Fields is null || request.Fields.Count == 0)
        {
            return "Fields must contain at least one entry.";
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            return "State must not be empty or whitespace.";
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in request.Fields)
        {
            if (field is null)
            {
                return "Fields must not contain null entries.";
            }

            if (string.IsNullOrWhiteSpace(field.Id))
            {
                return "Every field's Id must be non-blank.";
            }

            if (!IsValidFieldId(field.Id))
            {
                return $"Field id '{field.Id}' is invalid: it must start with a letter or underscore, contain only letters, digits and underscores, and be at most 64 characters.";
            }

            if (!seenIds.Add(field.Id))
            {
                return $"Duplicate field id '{field.Id}'.";
            }
        }

        foreach (var field in request.Fields)
        {
            var error = ValidateField(field);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates <paramref name="field"/> against its shape's structural rules.
    /// </summary>
    /// <param name="field">The field to validate.</param>
    /// <returns>
    /// A description of the first rule <paramref name="field"/> breaks, or <see langword="null"/>
    /// when it's valid.
    /// </returns>
    public static string? ValidateField(AIExtractionField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (string.IsNullOrWhiteSpace(field.Instructions))
        {
            return "Instructions must not be empty or whitespace.";
        }

        return field switch
        {
            AINumberExtractionField number => ValidateDecimals(number.Decimals),
            AIDurationExtractionField duration => ValidateDecimals(duration.Decimals),
            _ => null
        };
    }

    private static string? ValidateDecimals(int? decimals)
        => decimals is < MinDecimals or > MaxDecimals
            ? $"Decimals must be between {MinDecimals} and {MaxDecimals}."
            : null;
}
