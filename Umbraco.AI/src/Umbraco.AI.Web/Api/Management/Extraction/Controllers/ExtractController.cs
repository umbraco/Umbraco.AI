using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Extraction.Models;

#pragma warning disable UMBRACOAI_EXTRACTION // Consumes the experimental Extraction capability service

namespace Umbraco.AI.Web.Api.Management.Extraction.Controllers;

/// <summary>
/// Controller to extract typed values from text against an Extraction profile.
/// </summary>
[ApiVersion("1.0")]
public class ExtractController : ExtractionControllerBase
{
    private readonly IAIExtractionService _extractionService;
    private readonly IAIProfileService _profileService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractController"/> class.
    /// </summary>
    public ExtractController(
        IAIExtractionService extractionService,
        IAIProfileService profileService,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _extractionService = extractionService;
        _profileService = profileService;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Extract typed values for one or more fields from text.
    /// </summary>
    /// <param name="model">The extraction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One typed answer per field, keyed by field id and matching the field's <c>$type</c>.</returns>
    [HttpPost("extract")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ExtractResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Extract(
        [FromBody] ExtractRequestModel model,
        CancellationToken cancellationToken = default)
    {
        // Belt-and-suspenders alongside the shared AICapabilityGateFilter (applied via
        // ExtractionControllerBase's [AICapabilityGate(AICapability.Extraction)]): the filter protects
        // real HTTP traffic before body binding, but a caller invoking this action directly (as unit
        // tests do) bypasses the MVC filter pipeline, so the flag is also checked here.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Extraction))
        {
            return NotFound();
        }

        try
        {
            return await ExtractAsync(model, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // A named-but-missing profile is handled before the service is called, so an
            // InvalidOperationException reaching here is a configuration problem (no default Extraction
            // profile, or a resolved profile that isn't an Extraction profile), not a "not found".
            return BadRequest(new ProblemDetails
            {
                Type = "Error",
                Title = "Extraction failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(InvalidRequest(ex.Message));
        }
        catch (AIProviderException ex)
        {
            // Covers provider validation failures, other classified provider failures (auth, rate limit,
            // overloaded, network) and an inconsistent provider answer, surfaced the same way the
            // Decision endpoint surfaces them.
            return BadRequest(new ProblemDetails
            {
                Type = "Error",
                Title = "Extraction request failed",
                Detail = ex.UserMessage,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // Validation deliberately runs before profile resolution and any provider call, using the same
    // ExtractionFieldValidator rules ValidatingExtractionClient enforces for any C# caller. The
    // ArgumentException catch in Extract(...) remains as defence in depth.
    private async Task<IActionResult> ExtractAsync(ExtractRequestModel model, CancellationToken cancellationToken)
    {
        var mapped = TryMapRequest(model, out var request);
        if (mapped is not null)
        {
            return BadRequest(InvalidRequest(mapped));
        }

        var validationError = ExtractionFieldValidator.ValidateRequest(request!);
        if (validationError is not null)
        {
            return BadRequest(InvalidRequest(validationError));
        }

        Guid? profileId = null;
        if (!string.IsNullOrWhiteSpace(model.ProfileIdOrAlias))
        {
            var profile = await _profileService.GetProfileAsync(IdOrAlias.Parse(model.ProfileIdOrAlias, null), cancellationToken);
            if (profile is null)
            {
                return ProfileNotFound();
            }

            profileId = profile.Id;
        }

        var response = await _extractionService.GetExtractionResponseAsync(
            b => Configure(b, profileId), request!, cancellationToken: cancellationToken);

        return Ok(MapResponse(response));
    }

    // Ok(mapped) would lose the `$type` discriminators: OkObjectResult's constructor takes `object?`, so
    // MVC's output formatter falls back to the runtime type, which has no [JsonDerivedType] attributes of
    // its own. Setting DeclaredType makes the formatter serialize against the base contract.
    private IActionResult Ok(ExtractResponseModel mapped) =>
        new OkObjectResult(mapped) { DeclaredType = typeof(ExtractResponseModel) };

    private static void Configure(AIExtractionBuilder b, Guid? profileId)
    {
        b.WithAlias("management-api-extraction");
        if (profileId.HasValue)
        {
            b.WithProfile(profileId.Value);
        }
    }

    private static ProblemDetails InvalidRequest(string detail) => new()
    {
        Type = "Error",
        Title = "Invalid extraction request",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest
    };

    // Returns an error message, or null on success. Fields may still be null here (a JSON null entry
    // passes the model's `required`), so they are passed through for ExtractionFieldValidator to reject.
    private static string? TryMapRequest(ExtractRequestModel model, out AIExtractionRequest? request)
    {
        request = null;

        var fields = new List<AIExtractionField>();
        foreach (var field in model.Fields ?? [])
        {
            if (field is null)
            {
                fields.Add(null!);
                continue;
            }

            var error = TryMapField(field, out var mapped);
            if (error is not null)
            {
                return error;
            }

            fields.Add(mapped!);
        }

        request = new AIExtractionRequest
        {
            State = model.State,
            ReferenceDate = model.ReferenceDate,
            Fields = fields
        };
        return null;
    }

    private static string? TryMapField(ExtractionFieldModel model, out AIExtractionField? field)
    {
        field = null;
        switch (model)
        {
            case TextExtractionFieldModel:
                field = new AITextExtractionField { Id = model.Id, Instructions = model.Instructions };
                break;
            case NumberExtractionFieldModel number:
                field = new AINumberExtractionField
                {
                    Id = model.Id,
                    Instructions = model.Instructions,
                    Decimals = number.Decimals,
                    Integer = number.Integer,
                    Unit = number.Unit
                };
                break;
            case DateExtractionFieldModel:
                field = new AIDateExtractionField { Id = model.Id, Instructions = model.Instructions };
                break;
            case TimeExtractionFieldModel:
                field = new AITimeExtractionField { Id = model.Id, Instructions = model.Instructions };
                break;
            case DateTimeExtractionFieldModel:
                field = new AIDateTimeExtractionField { Id = model.Id, Instructions = model.Instructions };
                break;
            case DurationExtractionFieldModel duration:
                if (!TryParseName<AIDurationUnit>(duration.Unit, out var unit))
                {
                    return $"Unit '{duration.Unit}' is invalid for field '{model.Id}': use seconds, minutes, hours, days, weeks, months or years.";
                }

                field = new AIDurationExtractionField
                {
                    Id = model.Id,
                    Instructions = model.Instructions,
                    Unit = unit,
                    Decimals = duration.Decimals,
                    Integer = duration.Integer
                };
                break;
            case ListExtractionFieldModel list:
                var itemType = AIExtractionListItemType.Text;
                if (!string.IsNullOrWhiteSpace(list.ItemType)
                    && !TryParseName(list.ItemType, out itemType))
                {
                    return $"Item type '{list.ItemType}' is invalid for field '{model.Id}': use text, number or date.";
                }

                field = new AIListExtractionField { Id = model.Id, Instructions = model.Instructions, ItemType = itemType };
                break;
            default:
                return $"Unsupported field type '{model.GetType().Name}'.";
        }

        return null;
    }

    // Enum.TryParse also accepts numeric strings and comma-joined flags, so match the names only.
    private static bool TryParseName<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                result = Enum.Parse<TEnum>(name);
                return true;
            }
        }

        result = default;
        return false;
    }

    private static ExtractResponseModel MapResponse(AIExtractionResponse response) => new()
    {
        Answers = response.Answers.ToDictionary(a => a.Key, a => MapAnswer(a.Value)),
        ModelId = response.ModelId,
        Usage = MapUsage(response.Usage)
    };

    private static ExtractionAnswerModel MapAnswer(AIExtractionAnswer answer) => answer switch
    {
        AITextExtractionAnswer text => new TextExtractionAnswerModel
        {
            Value = text.Value,
            Span = text.Span is { } span ? MapSpan(span) : null,
            Confidence = text.Confidence,
            NullProbability = text.NullProbability,
            IsFound = text.IsFound
        },
        AINumberExtractionAnswer number => new NumberExtractionAnswerModel
        {
            Value = number.Value,
            Confidence = number.Confidence,
            NullProbability = number.NullProbability,
            IsFound = number.IsFound
        },
        AIDateExtractionAnswer date => new DateExtractionAnswerModel
        {
            Value = date.Value,
            Confidence = date.Confidence,
            NullProbability = date.NullProbability,
            IsFound = date.IsFound
        },
        AITimeExtractionAnswer time => new TimeExtractionAnswerModel
        {
            Value = time.Value,
            Confidence = time.Confidence,
            NullProbability = time.NullProbability,
            IsFound = time.IsFound
        },
        AIDateTimeExtractionAnswer dateTime => new DateTimeExtractionAnswerModel
        {
            Value = dateTime.Value,
            Confidence = dateTime.Confidence,
            NullProbability = dateTime.NullProbability,
            IsFound = dateTime.IsFound
        },
        AIDurationExtractionAnswer duration => new DurationExtractionAnswerModel
        {
            Value = duration.Value,
            Unit = duration.Unit.ToString().ToLowerInvariant(),
            Confidence = duration.Confidence,
            NullProbability = duration.NullProbability,
            IsFound = duration.IsFound
        },
        AIListExtractionAnswer list => new ListExtractionAnswerModel
        {
            Values = list.Values,
            Spans = list.Spans.Select(MapSpan).ToList(),
            Confidence = list.Confidence,
            NullProbability = list.NullProbability,
            IsFound = list.IsFound
        },
        _ => throw new InvalidOperationException($"Unsupported extraction answer type '{answer.GetType().Name}'.")
    };

    private static ExtractionSpanModel MapSpan(AIExtractionSpan span) => new() { Start = span.Start, End = span.End };

    private static UsageModel? MapUsage(UsageDetails? usage) => usage is null
        ? null
        : new UsageModel
        {
            InputTokens = usage.InputTokenCount,
            OutputTokens = usage.OutputTokenCount,
            TotalTokens = usage.TotalTokenCount
        };
}
