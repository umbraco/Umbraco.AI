using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Decision.Models;

#pragma warning disable UMBRACOAI_DECISION // Consumes the experimental Decision capability service

namespace Umbraco.AI.Web.Api.Management.Decision.Controllers;

/// <summary>
/// Controller to ask a Decision question against a Decision profile.
/// </summary>
[ApiVersion("1.0")]
public class AskDecisionController : DecisionControllerBase
{
    // Only one question is ever sent — its id is purely a correlation key for
    // IAIDecisionService.GetDecisionResponseAsync's keyed Answers, never surfaced on the wire, so a
    // fixed value is fine.
    private const string QuestionId = "ask";

    private readonly IAIDecisionService _decisionService;
    private readonly IAIProfileService _profileService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AskDecisionController"/> class.
    /// </summary>
    public AskDecisionController(
        IAIDecisionService decisionService,
        IAIProfileService profileService,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _decisionService = decisionService;
        _profileService = profileService;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Ask a binary, choice, or score Decision question.
    /// </summary>
    /// <param name="model">The Decision request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed Decision response, matching the question's <c>$type</c>.</returns>
    [HttpPost("ask")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(DecisionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask(
        [FromBody] AskDecisionRequestModel model,
        CancellationToken cancellationToken = default)
    {
        // Belt-and-suspenders alongside the shared AICapabilityGateFilter (applied via
        // DecisionControllerBase's [AICapabilityGate(AICapability.Decision)]): that filter protects
        // real HTTP traffic (it runs before body model binding, which a polymorphic $type can
        // otherwise fail), but a caller invoking this action directly (as unit tests do) bypasses the
        // MVC filter pipeline entirely, so the flag is also checked here.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return NotFound();
        }

        try
        {
            return await AskAsync(model, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // A supplied-but-missing profile is already handled above via TryGetProfileIdAsync before
            // the service is ever called, so any InvalidOperationException reaching here is a
            // configuration problem (no default Decision profile, or a resolved profile that isn't a
            // Decision profile) rather than a "not found" the caller can fix by retrying — see SPEC.md's
            // guarantees for POST decision/ask.
            return BadRequest(new ProblemDetails
            {
                Title = "Decision failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid question",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (AIProviderException ex)
        {
            // Covers a provider validation failure (e.g. Jev 422, AIProviderErrorCategory.InvalidRequest),
            // other classified provider failures (auth, rate limit, overloaded, network), and an
            // inconsistent/incomplete provider answer caught by AIErrorClassifyingDecisionClient (see
            // ARCHITECTURE.md's "Checks") — all surfaced the same way GenerateImageController surfaces its
            // own provider-level failures.
            return BadRequest(new ProblemDetails
            {
                Title = "Decision request failed",
                Detail = ex.UserMessage,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // Validates the already-mapped Core question via the shared Umbraco.AI.Core.Decision.
    // DecisionQuestionValidator (the same rules ValidatingDecisionClient enforces for any C# caller),
    // then resolves the profile and calls the service with a one-question batch request. Validation
    // deliberately runs first — before profile resolution and any provider call — see ARCHITECTURE.md's
    // Security section and SPEC.md's guarantees for POST decision/ask. The ArgumentException catch in
    // Ask(...) remains as defence in depth in case the mapped question still reaches
    // ValidatingDecisionClient with something this doesn't parse for.
    private async Task<IActionResult> AskAsync(AskDecisionRequestModel model, CancellationToken cancellationToken)
    {
        var question = MapQuestion(model.Question);

        var validationError = DecisionQuestionValidator.ValidateQuestion(question);
        if (validationError is not null)
        {
            return BadRequest(InvalidQuestion(validationError));
        }

        var profileId = await TryResolveProfileAsync(model.ProfileIdOrAlias, cancellationToken);
        if (profileId.IsFailure)
        {
            return profileId.FailureResult!;
        }

        var request = new AIDecisionRequest { State = model.State, Questions = [question] };

        AIDecisionResponse response = await _decisionService.GetDecisionResponseAsync(
            b => Configure(b, profileId.ProfileId), request, cancellationToken: cancellationToken);

        if (!response.Answers.TryGetValue(QuestionId, out var answer))
        {
            throw new InvalidOperationException("The AI provider did not answer the question.");
        }

        return Ok(MapResponse(answer, response.ModelId, response.Usage));
    }

    private IActionResult Ok(DecisionResponseModel mapped) =>
        // Ok(mapped) would lose the `$type` discriminator: OkObjectResult's constructor takes
        // `object? value`, so the compile-time type DecisionResponseModel is erased and MVC's output
        // formatter falls back to the *runtime* type (e.g. BinaryDecisionResponseModel) — a type with
        // no [JsonDerivedType] attributes of its own, so System.Text.Json never emits "$type". Setting
        // DeclaredType explicitly (the same field ActionResult<T>.Convert() sets from typeof(TValue))
        // tells the formatter to serialize against the base contract instead, which is where
        // [JsonPolymorphic]/[JsonDerivedType] are declared. See SPEC.md's guarantees for POST decision/ask.
        new OkObjectResult(mapped) { DeclaredType = typeof(DecisionResponseModel) };

    private async Task<ProfileResolution> TryResolveProfileAsync(string? profileIdOrAlias, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profileIdOrAlias))
        {
            return ProfileResolution.Success(null);
        }

        var profileId = await _profileService.TryGetProfileIdAsync(IdOrAlias.Parse(profileIdOrAlias, null), cancellationToken);
        return profileId.HasValue
            ? ProfileResolution.Success(profileId)
            : ProfileResolution.Failure(ProfileNotFound());
    }

    private static void Configure(AIDecisionBuilder b, Guid? profileId)
    {
        b.WithAlias("management-api-decision");
        if (profileId.HasValue)
        {
            b.WithProfile(profileId.Value);
        }
    }

    private readonly struct ProfileResolution
    {
        private ProfileResolution(Guid? profileId, IActionResult? failureResult)
        {
            ProfileId = profileId;
            FailureResult = failureResult;
        }

        public Guid? ProfileId { get; }

        public IActionResult? FailureResult { get; }

        public bool IsFailure => FailureResult is not null;

        public static ProfileResolution Success(Guid? profileId) => new(profileId, null);

        public static ProfileResolution Failure(IActionResult result) => new(null, result);
    }

    private static ProblemDetails InvalidQuestion(string detail) => new()
    {
        Title = "Invalid question",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest
    };

    // Maps the wire question to the matching Core question type, assigning the fixed QuestionId — the
    // question's own concrete type is the discriminator on both sides (see ARCHITECTURE.md's "Core
    // types"), so one switch expression replaces what used to be three parallel per-kind controller
    // methods (AskAsync/AskAsync/AskScoreAsync).
    private static AIDecisionQuestion MapQuestion(DecisionQuestionModel model) => model switch
    {
        BinaryDecisionQuestionModel binary => new AIBinaryDecisionQuestion
        {
            Id = QuestionId,
            Instructions = binary.Instructions,
            TrueCriteria = binary.TrueCriteria,
            FalseCriteria = binary.FalseCriteria
        },
        ChoiceDecisionQuestionModel choice => new AIChoiceDecisionQuestion
        {
            Id = QuestionId,
            Instructions = choice.Instructions,
            // Options (and its entries) may still be null here despite the Web model's `required` — that
            // keyword only enforces the JSON property's presence, not a non-null value — so null is passed
            // through rather than dereferenced, letting DecisionQuestionValidator reject it uniformly.
            Options = choice.Options?.Select(o => o is null ? null! : new AIDecisionOption(o.Key, o.Description)).ToList()!
        },
        ScoreDecisionQuestionModel score => new AIScoreDecisionQuestion
        {
            Id = QuestionId,
            Instructions = score.Instructions,
            // Levels may still be null here for the same reason Options may be — see above.
            Levels = score.Levels?.Select(l => l is null ? null! : new AIDecisionScoreLevel(l.Description)).ToList()!
        },
        _ => throw new InvalidOperationException($"Unsupported decision question type '{model.GetType().Name}'.")
    };

    // The single answer's own concrete type is the discriminator on the way out too — mirrors
    // MapQuestion above.
    private static DecisionResponseModel MapResponse(AIDecisionAnswer answer, string? modelId, UsageDetails? usage) => answer switch
    {
        AIBinaryDecisionAnswer binary => new BinaryDecisionResponseModel
        {
            TrueProbability = binary.TrueProbability,
            ModelId = modelId,
            Usage = MapUsage(usage)
        },
        AIChoiceDecisionAnswer choice => new ChoiceDecisionResponseModel
        {
            Choice = choice.Choice,
            Confidence = choice.Confidence,
            Probabilities = choice.Probabilities,
            ModelId = modelId,
            Usage = MapUsage(usage)
        },
        AIScoreDecisionAnswer score => new ScoreDecisionResponseModel
        {
            Score = score.Score,
            Confidence = score.Confidence,
            Probabilities = score.Probabilities,
            ModelId = modelId,
            Usage = MapUsage(usage)
        },
        _ => throw new InvalidOperationException($"Unsupported decision answer type '{answer.GetType().Name}'.")
    };

    private static UsageModel? MapUsage(UsageDetails? usage) => usage is null
        ? null
        : new UsageModel
        {
            InputTokens = usage.InputTokenCount,
            OutputTokens = usage.OutputTokenCount,
            TotalTokens = usage.TotalTokenCount
        };
}
