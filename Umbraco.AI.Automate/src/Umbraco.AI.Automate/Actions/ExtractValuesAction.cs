using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Automate.Helpers;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;

#pragma warning disable UMBRACOAI_EXTRACTION // Extraction capability is experimental

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that pulls several typed values (text, numbers, dates, durations, lists) out
/// of a piece of text against an AI extraction profile in a single call. Every configured alias is
/// always present in the output as <c>{ value, found, confidence }</c>, so a later step can branch
/// on <c>found</c> reliably. Unlike <see cref="AskDecisionsAction"/>, a field that wasn't found
/// still gets an output.
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.ExtractValues, "Extract Values",
    Description = "Extracts typed values (text, numbers, dates, lists) from text against an AI extraction profile, in a single call.",
    Group = "AI",
    Icon = "icon-filter")]
public sealed class ExtractValuesAction : DynamicOutputActionBase<ExtractValuesSettings>
{
    private const int MaxFields = 20;

    // An ISO date, optionally followed by a time part. Anything else (e.g. "next week", "20/10/2026") is rejected.
    private static readonly Regex IsoDatePrefix = new(@"^\d{4}-\d{2}-\d{2}(?:T.+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAIExtractionService _extractionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExtractValuesAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractValuesAction"/> class.
    /// </summary>
    public ExtractValuesAction(
        ActionInfrastructure infrastructure,
        IAIExtractionService extractionService,
        IAIExperimentalFeatures experimentalFeatures,
        TimeProvider timeProvider,
        ILogger<ExtractValuesAction> logger)
        : base(infrastructure)
    {
        _extractionService = extractionService;
        _experimentalFeatures = experimentalFeatures;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // Flag off at run time (e.g. flipped without a restart): refuse before any provider
        // call, even though the action is also excluded from discovery at compose time when
        // the flag is off at startup.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Extraction))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Extraction is disabled."),
                StepRunErrorCategory.Validation);
        }

        var settings = context.GetSettings<ExtractValuesSettings>();

        // Only rules this action owns are checked here (the editor's field cap, the kinds it
        // maps, the reference date). Everything Core's extraction validator already enforces
        // (aliases, empty content, no fields, decimals) surfaces as its ArgumentException below.
        var validationError = ValidateSettings(settings, out var referenceDate);
        if (validationError is not null)
        {
            return ActionResult.Failed(
                new ArgumentException(validationError, nameof(settings)),
                StepRunErrorCategory.Validation);
        }

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Extracting {FieldCount} field(s)",
            context.AutomationId, context.RunId, settings.Fields.Count);

        try
        {
            var request = new AIExtractionRequest
            {
                State = settings.Context,
                Fields = settings.Fields.Select(MapField).ToList(),
                ReferenceDate = referenceDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
            };

            var response = await _extractionService.GetExtractionResponseAsync(
                configure: b =>
                {
                    b.WithAlias("automate-extract-values");

                    if (settings.ProfileId.HasValue && settings.ProfileId.Value != Guid.Empty)
                    {
                        b.WithProfile(settings.ProfileId.Value);
                    }
                },
                request: request,
                cancellationToken: cancellationToken);

            return Success(BuildOutput(settings.Fields, response.Answers));
        }
        catch (ArgumentException ex)
        {
            // Thrown by Core's validating extraction client (aliases, empty content, decimals ...).
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (InvalidOperationException ex)
        {
            // Thrown by IAIExtractionService when no default Extraction profile resolves, or the
            // configured profile isn't an Extraction profile.
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (OperationCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Automation {AutomationId} / Run {RunId}: Extract values failed",
                context.AutomationId, context.RunId);
            return ActionResult.Failed(ex, StepErrorCategoryMapping.FromException(ex));
        }
    }

    /// <summary>
    /// Resolves the output schema from the configured fields: one object property per alias with
    /// <c>value</c>, <c>found</c> and <c>confidence</c>. Returns <see langword="null"/> when no
    /// field has a usable alias and kind yet.
    /// </summary>
    protected override Task<JsonSchema?> GetOutputSchemaAsync(
        ExtractValuesSettings? settings,
        CancellationToken cancellationToken = default)
    {
        if (settings?.Fields is not { Count: > 0 } fields)
        {
            return Task.FromResult<JsonSchema?>(null);
        }

        var properties = new JsonObject();
        foreach (var field in fields)
        {
            if (field is null || string.IsNullOrWhiteSpace(field.Alias) || !ExtractionFieldValidator.IsValidFieldId(field.Alias))
            {
                continue;
            }

            var valueSchema = ValueSchema(field.Kind);
            if (valueSchema is null)
            {
                continue;
            }

            properties[field.Alias] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["value"] = valueSchema,
                    ["found"] = new JsonObject { ["type"] = "boolean" },
                    ["confidence"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
                },
            };
        }

        if (properties.Count == 0)
        {
            return Task.FromResult<JsonSchema?>(null);
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
        return Task.FromResult<JsonSchema?>(JsonSchema.FromText(schema.ToJsonString()));
    }

    private static JsonObject? ValueSchema(string? kind) => kind switch
    {
        "text" or "date" or "time" or "datetime" => new JsonObject { ["type"] = "string" },
        "number" or "duration" => new JsonObject { ["type"] = "number" },
        "list" => new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonObject { ["type"] = "string" },
        },
        _ => null,
    };

    /// <summary>
    /// Validates what this action owns: the editor's 20-field cap, a recognized kind (and
    /// duration unit / list item type) on every field, and the reference date format.
    /// </summary>
    private static string? ValidateSettings(ExtractValuesSettings settings, out DateOnly? referenceDate)
    {
        referenceDate = null;

        if (settings.Fields is null)
        {
            return "Fields must not be empty.";
        }

        if (settings.Fields.Count > MaxFields)
        {
            return $"Fields must contain no more than {MaxFields} entries.";
        }

        foreach (var field in settings.Fields)
        {
            if (field is null)
            {
                return "Fields must not contain null entries.";
            }

            if (ValueSchema(field.Kind) is null)
            {
                return $"Unknown field kind '{field.Kind}' for alias '{field.Alias}'.";
            }

            if (field.Kind == "duration" && !TryParseDurationUnit(field.Unit, out _))
            {
                return $"Duration field '{field.Alias}' needs a unit of seconds, minutes, hours, days, weeks, months or years.";
            }

            if (field.Kind == "list" && !TryParseItemType(field.ItemType, out _))
            {
                return $"List field '{field.Alias}' has an unknown item type '{field.ItemType}'. Use text, number or date.";
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.ReferenceDate))
        {
            if (!TryParseReferenceDate(settings.ReferenceDate, out var parsed))
            {
                return $"Reference date '{settings.ReferenceDate}' must be an ISO date (yyyy-MM-dd) or an ISO date-time.";
            }

            referenceDate = parsed;
        }

        return null;
    }

    private static bool TryParseReferenceDate(string value, out DateOnly date)
    {
        date = default;
        var trimmed = value.Trim();
        if (!IsoDatePrefix.IsMatch(trimmed))
        {
            return false;
        }

        if (!DateOnly.TryParseExact(trimmed[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return false;
        }

        // The date part is used as written, so a time zone offset never shifts the day.
        return trimmed.Length == 10
            || DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
    }

    private static bool TryParseDurationUnit(string? value, out AIDurationUnit unit)
        => ExtractionEnumNames.TryParse(value, out unit);

    private static bool TryParseItemType(string? value, out AIExtractionListItemType itemType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            itemType = AIExtractionListItemType.Text;
            return true;
        }

        return ExtractionEnumNames.TryParse(value, out itemType);
    }

    /// <summary>
    /// Maps a flat <see cref="ExtractValuesField"/> to the typed Core field its kind calls for, with
    /// <see cref="AIExtractionField.Id"/> set to its alias. <see cref="ValidateSettings"/> already
    /// rejected unrecognized kinds, units and item types.
    /// </summary>
    private static AIExtractionField MapField(ExtractValuesField field) => field.Kind switch
    {
        "number" => new AINumberExtractionField
        {
            Id = field.Alias,
            Instructions = field.Instructions,
            Decimals = field.Decimals,
            Integer = field.Integer,
            Unit = string.IsNullOrWhiteSpace(field.Unit) ? null : field.Unit,
        },
        "date" => new AIDateExtractionField { Id = field.Alias, Instructions = field.Instructions },
        "time" => new AITimeExtractionField { Id = field.Alias, Instructions = field.Instructions },
        "datetime" => new AIDateTimeExtractionField { Id = field.Alias, Instructions = field.Instructions },
        "duration" => new AIDurationExtractionField
        {
            Id = field.Alias,
            Instructions = field.Instructions,
            Decimals = field.Decimals,
            Integer = field.Integer,
            Unit = TryParseDurationUnit(field.Unit, out var unit) ? unit : default,
        },
        "list" => new AIListExtractionField
        {
            Id = field.Alias,
            Instructions = field.Instructions,
            ItemType = TryParseItemType(field.ItemType, out var itemType) ? itemType : AIExtractionListItemType.Text,
        },
        _ => new AITextExtractionField { Id = field.Alias, Instructions = field.Instructions },
    };

    /// <summary>
    /// Builds the output object: one <c>{ value, found, confidence }</c> entry per configured
    /// alias, including aliases the provider returned no answer for.
    /// </summary>
    private static Dictionary<string, object> BuildOutput(
        IReadOnlyList<ExtractValuesField> fields,
        IReadOnlyDictionary<string, AIExtractionAnswer> answers)
    {
        var output = new Dictionary<string, object>(fields.Count);
        foreach (var field in fields)
        {
            output[field.Alias] = answers.TryGetValue(field.Alias, out var answer)
                ? new ExtractedOutput(ValueOf(answer), answer.IsFound, answer.Confidence)
                : new ExtractedOutput(null, false, null);
        }

        return output;
    }

    /// <summary>
    /// The answer's value in its output shape. Dates and times are ISO strings, formatted
    /// culture-invariant, so Automate's string comparison orders them correctly.
    /// </summary>
    private static object? ValueOf(AIExtractionAnswer answer) => answer switch
    {
        AITextExtractionAnswer text => text.Value,
        AINumberExtractionAnswer number => number.Value,
        AIDateExtractionAnswer date => date.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        AITimeExtractionAnswer time => time.Value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        AIDateTimeExtractionAnswer dateTime => dateTime.Value?.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture),
        AIDurationExtractionAnswer duration => duration.Value,
        AIListExtractionAnswer list => list.Values,
        _ => throw new InvalidOperationException($"Unexpected answer type '{answer.GetType().Name}'."),
    };

    private sealed record ExtractedOutput(object? Value, bool Found, double? Confidence);
}
