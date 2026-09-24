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
        // Belt-and-suspenders alongside DecisionCapabilityGateFilter: that filter protects real HTTP
        // traffic (it runs before body model binding, which a polymorphic $type can otherwise fail),
        // but a caller invoking this action directly (as unit tests do) bypasses the MVC filter
        // pipeline entirely, so the flag is also checked here.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return NotFound();
        }

        try
        {
            return model.Question switch
            {
                BinaryDecisionQuestionModel binary
                    => await AskAsync(MapQuestion(binary), model.ProfileIdOrAlias, cancellationToken),
                ChoiceDecisionQuestionModel choice
                    => await AskAsync(MapQuestion(choice), model.ProfileIdOrAlias, cancellationToken),
                ScoreDecisionQuestionModel score
                    => await AskAsync(MapQuestion(score), model.ProfileIdOrAlias, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported decision question type '{model.Question.GetType().Name}'.")
            };
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
            // Covers both a provider validation failure (e.g. Jev 422, AIProviderErrorCategory.InvalidRequest)
            // and other classified provider failures (auth, rate limit, overloaded, network) — all surfaced
            // the same way GenerateImageController surfaces its own provider-level failures.
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
    // then resolves the profile and calls the service. Validation deliberately runs first — before
    // profile resolution and any provider call — see ARCHITECTURE.md's Security section and SPEC.md's
    // guarantees for POST decision/ask. The ArgumentException catch below remains as defence in depth
    // in case the mapped question still reaches ValidatingDecisionClient with something this doesn't
    // parse for.
    private async Task<IActionResult> AskAsync<TResponse>(
        AIDecisionQuestion<TResponse> question,
        string? profileIdOrAlias,
        CancellationToken cancellationToken)
        where TResponse : AIDecisionResponse
    {
        var validationError = DecisionQuestionValidator.Validate(question);
        if (validationError is not null)
        {
            return BadRequest(InvalidQuestion(validationError));
        }

        Guid? profileId = null;
        if (!string.IsNullOrWhiteSpace(profileIdOrAlias))
        {
            profileId = await _profileService.TryGetProfileIdAsync(IdOrAlias.Parse(profileIdOrAlias, null), cancellationToken);
            if (!profileId.HasValue)
            {
                return ProfileNotFound();
            }
        }

        void Configure(AIDecisionBuilder b)
        {
            b.WithAlias("management-api-decision");
            if (profileId.HasValue)
            {
                b.WithProfile(profileId.Value);
            }
        }

        TResponse response = await _decisionService.AskAsync(Configure, question, cancellationToken);
        DecisionResponseModel mapped = response switch
        {
            AIBinaryDecisionResponse binary => MapResponse(binary),
            AIChoiceDecisionResponse choice => MapResponse(choice),
            AIScoreDecisionResponse score => MapResponse(score),
            _ => throw new InvalidOperationException($"Unsupported decision response type '{response.GetType().Name}'.")
        };

        return Ok(mapped);
    }

    private static ProblemDetails InvalidQuestion(string detail) => new()
    {
        Title = "Invalid question",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest
    };

    private static AIBinaryDecisionQuestion MapQuestion(BinaryDecisionQuestionModel model) => new()
    {
        Instructions = model.Instructions,
        Context = model.Context,
        TrueCriteria = model.TrueCriteria,
        FalseCriteria = model.FalseCriteria
    };

    private static AIChoiceDecisionQuestion MapQuestion(ChoiceDecisionQuestionModel model) => new()
    {
        Instructions = model.Instructions,
        Context = model.Context,
        // Options (and its entries) may still be null here despite the Web model's `required` — that
        // keyword only enforces the JSON property's presence, not a non-null value — so null is passed
        // through rather than dereferenced, letting DecisionQuestionValidator reject it uniformly.
        Options = model.Options?.Select(o => o is null ? null! : new AIDecisionOption(o.Key, o.Description)).ToList()!
    };

    private static AIScoreDecisionQuestion MapQuestion(ScoreDecisionQuestionModel model) => new()
    {
        Instructions = model.Instructions,
        Context = model.Context,
        // Levels may still be null here for the same reason Options may be — see above.
        Levels = model.Levels!
    };

    private static BinaryDecisionResponseModel MapResponse(AIBinaryDecisionResponse response) => new()
    {
        Answer = response.Answer,
        Probability = response.Probability,
        Confidence = response.Confidence,
        ModelId = response.ModelId,
        Usage = MapUsage(response.Usage)
    };

    private static ChoiceDecisionResponseModel MapResponse(AIChoiceDecisionResponse response) => new()
    {
        Choice = response.Choice,
        Confidence = response.Confidence,
        Probabilities = response.Probabilities,
        ModelId = response.ModelId,
        Usage = MapUsage(response.Usage)
    };

    private static ScoreDecisionResponseModel MapResponse(AIScoreDecisionResponse response) => new()
    {
        Score = response.Score,
        Level = response.Level,
        Confidence = response.Confidence,
        Probabilities = response.Probabilities,
        ModelId = response.ModelId,
        Usage = MapUsage(response.Usage)
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
