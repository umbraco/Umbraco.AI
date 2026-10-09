using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;

#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that asks several questions about the same content against an AI decision
/// profile in a single call, so later steps can branch on each answer (e.g. feeding an If or
/// Switch step per question alias). Unlike the three single-question actions, the questions
/// themselves aren't bindable — only <see cref="AskDecisionsSettings.Context"/> is.
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.AskDecisions, "Ask Questions",
    Description = "Asks several questions about the same content against an AI decision profile, in a single call.",
    Group = "AI",
    Icon = "icon-speed-gauge")]
public sealed class AskDecisionsAction : DynamicOutputActionBase<AskDecisionsSettings>
{
    private const int MinQuestions = 1;
    private const int MaxQuestions = 20;

    private static readonly Regex AliasPattern = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly IAIDecisionService _decisionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly ILogger<AskDecisionsAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AskDecisionsAction"/> class.
    /// </summary>
    public AskDecisionsAction(
        ActionInfrastructure infrastructure,
        IAIDecisionService decisionService,
        IAIExperimentalFeatures experimentalFeatures,
        ILogger<AskDecisionsAction> logger)
        : base(infrastructure)
    {
        _decisionService = decisionService;
        _experimentalFeatures = experimentalFeatures;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // Flag off at run time (e.g. flipped without a restart): refuse before any provider
        // call, even though the action is also excluded from discovery at compose time when
        // the flag is off at startup.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Decision is disabled."),
                StepRunErrorCategory.Validation);
        }

        var settings = context.GetSettings<AskDecisionsSettings>();

        var validationError = ValidateQuestions(settings.Questions);
        if (validationError is not null)
        {
            return ActionResult.Failed(
                new ArgumentException(validationError, nameof(settings.Questions)),
                StepRunErrorCategory.Validation);
        }

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Asking {QuestionCount} decision question(s)",
            context.AutomationId, context.RunId, settings.Questions.Count);

        try
        {
            var request = new AIDecisionRequest
            {
                State = settings.Context,
                Questions = settings.Questions.Select(MapQuestion).ToList(),
            };

            var response = await _decisionService.GetDecisionResponseAsync(
                configure: b =>
                {
                    b.WithAlias("automate-ask-decisions");

                    if (settings.ProfileId.HasValue && settings.ProfileId.Value != Guid.Empty)
                    {
                        b.WithProfile(settings.ProfileId.Value);
                    }
                },
                request: request,
                cancellationToken: cancellationToken);

            return Success(BuildOutput(settings.Questions, response.Answers));
        }
        catch (ArgumentException ex)
        {
            // Thrown by Core's ValidatingDecisionClient (e.g. fewer than 2 choice options).
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (InvalidOperationException ex)
        {
            // Thrown by IAIDecisionService when no default Decision profile resolves, or the
            // configured profile isn't a Decision profile.
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (OperationCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Automation {AutomationId} / Run {RunId}: Ask questions decision failed",
                context.AutomationId, context.RunId);
            return ActionResult.Failed(ex, StepRunErrorCategory.Unknown);
        }
    }

    /// <summary>
    /// Resolves the output schema from the configured questions, one object property per alias
    /// describing that question's kind's fields, so the binding UI lists
    /// <c>refund.answer</c>/<c>category.choice</c>/etc. Returns <see langword="null"/> when no
    /// question has a usable alias and kind yet (e.g. settings not configured).
    /// </summary>
    protected override Task<JsonSchema?> GetOutputSchemaAsync(
        AskDecisionsSettings? settings,
        CancellationToken cancellationToken = default)
    {
        if (settings?.Questions is not { Count: > 0 } questions)
        {
            return Task.FromResult<JsonSchema?>(null);
        }

        var properties = new JsonObject();
        foreach (var question in questions)
        {
            if (question is null || string.IsNullOrWhiteSpace(question.Alias) || !AliasPattern.IsMatch(question.Alias))
            {
                continue;
            }

            var fieldSchema = QuestionOutputSchema(question);
            if (fieldSchema is not null)
            {
                properties[question.Alias] = fieldSchema;
            }
        }

        if (properties.Count == 0)
        {
            return Task.FromResult<JsonSchema?>(null);
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
        return Task.FromResult<JsonSchema?>(JsonSchema.FromText(schema.ToJsonString()));
    }

    /// <summary>
    /// A nullable number field, matching how the single-question actions' own static output
    /// schemas render a nullable <c>double?</c> property (e.g. <c>AskChoiceDecisionOutput.Confidence</c>)
    /// via <c>Json.Schema.Generation</c>'s reflection-based builder: <c>{ "type": ["number", "null"] }</c>,
    /// not a plain <c>"number"</c>.
    /// </summary>
    private static JsonObject NullableNumberField() => new() { ["type"] = new JsonArray("number", "null") };

    /// <summary>
    /// Builds the output object schema for a single question's kind, or <see langword="null"/>
    /// for an unrecognized kind. The caller already filters out questions with no usable alias.
    /// </summary>
    private static JsonObject? QuestionOutputSchema(AskDecisionsQuestion question) => question.Kind switch
    {
        "binary" => new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["answer"] = new JsonObject { ["type"] = "boolean" },
                ["probability"] = new JsonObject { ["type"] = "number" },
            },
        },
        "choice" => new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["choice"] = new JsonObject { ["type"] = "string" },
                ["confidence"] = NullableNumberField(),
            },
        },
        "score" => new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["score"] = new JsonObject { ["type"] = "number" },
                ["level"] = new JsonObject { ["type"] = "string" },
                ["confidence"] = NullableNumberField(),
            },
        },
        _ => null,
    };

    /// <summary>
    /// Validates <paramref name="questions"/>' batch shape before any provider call: 1 to 20
    /// entries, every alias non-blank/unique/shaped like an identifier, every kind recognized,
    /// and every binary question's threshold within 0.0 to 1.0. Per-kind bounds (choice option
    /// count, score level count) are Automate-agnostic Decision rules, so they're left to Core's
    /// Decision validator (<see cref="Umbraco.AI.Core.Decision.AIDecisionRequest"/>'s single
    /// source of truth) rather than duplicated here — its <see cref="ArgumentException"/> is
    /// caught in <see cref="ExecuteAsync"/> and mapped to <see cref="StepRunErrorCategory.Validation"/>,
    /// the same way <c>AskChoiceDecisionAction</c> relies on it for its own 2..255 check.
    /// </summary>
    /// <returns>A description of the first rule broken, or <see langword="null"/> when valid.</returns>
    private static string? ValidateQuestions(IReadOnlyList<AskDecisionsQuestion>? questions)
    {
        if (questions is null || questions.Count is < MinQuestions or > MaxQuestions)
        {
            return $"Questions must contain between {MinQuestions} and {MaxQuestions} entries.";
        }

        var seenAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in questions)
        {
            if (question is null)
            {
                return "Questions must not contain null entries.";
            }

            if (string.IsNullOrWhiteSpace(question.Alias) || !AliasPattern.IsMatch(question.Alias))
            {
                return $"Question alias '{question.Alias}' must start with a letter and contain only letters, digits, or underscores.";
            }

            if (!seenAliases.Add(question.Alias))
            {
                return $"Duplicate question alias '{question.Alias}'.";
            }

            if (question.Kind is not ("binary" or "choice" or "score"))
            {
                return $"Unknown question kind '{question.Kind}' for alias '{question.Alias}'.";
            }

            if (question.Kind == "binary" && (double.IsNaN(question.Threshold) || question.Threshold is < 0.0 or > 1.0))
            {
                return $"Threshold for '{question.Alias}' must be between 0.0 and 1.0.";
            }
        }

        return null;
    }

    /// <summary>
    /// Maps a flat <see cref="AskDecisionsQuestion"/> to the typed <see cref="AIDecisionQuestion"/>
    /// its <see cref="AskDecisionsQuestion.Kind"/> calls for, with <see cref="AIDecisionQuestion.Id"/>
    /// set to its alias. <see cref="ValidateQuestions"/> already rejected unrecognized kinds, so
    /// anything other than <c>"choice"</c>/<c>"score"</c> reaching here is <c>"binary"</c>.
    /// </summary>
    private static AIDecisionQuestion MapQuestion(AskDecisionsQuestion question) => question.Kind switch
    {
        "choice" => new AIChoiceDecisionQuestion
        {
            Id = question.Alias,
            Instructions = question.Instructions,
            Options = MapOptions(question.Options),
        },
        "score" => new AIScoreDecisionQuestion
        {
            Id = question.Alias,
            Instructions = question.Instructions,
            Levels = MapLevels(question.Levels),
        },
        _ => new AIBinaryDecisionQuestion
        {
            Id = question.Alias,
            Instructions = question.Instructions,
            TrueCriteria = question.TrueCriteria,
            FalseCriteria = question.FalseCriteria,
        },
    };

    /// <summary>
    /// Maps <see cref="AskDecisionsQuestion.Options"/>'s key/value rows into
    /// <see cref="AIDecisionOption"/> entries, in order, trimming each key and value — mirrors
    /// <c>AskChoiceDecisionAction.MapOptions</c>. Entry count and duplicate-key checks happen
    /// downstream, in Core's Decision validator.
    /// </summary>
    private static IReadOnlyList<AIDecisionOption> MapOptions(IReadOnlyList<AskChoiceDecisionOption>? options)
    {
        if (options is null || options.Count == 0)
        {
            return [];
        }

        var mapped = new List<AIDecisionOption>(options.Count);
        foreach (var option in options)
        {
            var key = option?.Key?.Trim() ?? string.Empty;
            var value = option?.Value?.Trim();
            mapped.Add(new AIDecisionOption(key, string.IsNullOrEmpty(value) ? null : value));
        }

        return mapped;
    }

    /// <summary>
    /// Maps <see cref="AskDecisionsQuestion.Levels"/>'s labels, lowest first, into
    /// <see cref="AIDecisionScoreLevel"/> entries in the same order — mirrors
    /// <c>AskScoreDecisionAction.MapLevels</c>. Entry count and blank-label checks happen
    /// downstream, in Core's Decision validator.
    /// </summary>
    private static IReadOnlyList<AIDecisionScoreLevel> MapLevels(IReadOnlyList<string>? levels)
    {
        if (levels is null || levels.Count == 0)
        {
            return [];
        }

        var mapped = new List<AIDecisionScoreLevel>(levels.Count);
        foreach (var level in levels)
        {
            mapped.Add(new AIDecisionScoreLevel(level ?? string.Empty));
        }

        return mapped;
    }

    /// <summary>
    /// Builds the output object, one entry per question keyed by its alias.
    /// </summary>
    private static object BuildOutput(
        IReadOnlyList<AskDecisionsQuestion> questions,
        IReadOnlyDictionary<string, AIDecisionAnswer> answers)
    {
        var output = new Dictionary<string, object>(questions.Count);
        foreach (var question in questions)
        {
            if (answers.TryGetValue(question.Alias, out var answer))
            {
                output[question.Alias] = MapAnswer(question, answer);
            }
        }

        return output;
    }

    /// <summary>
    /// Maps a single typed answer to its output shape, matching the kind's fields in
    /// <see cref="QuestionOutputSchema"/>. The nearest level label for a score answer is found
    /// the same way as <c>AskScoreDecisionAction.NearestLevelLabel</c>: the fractional score
    /// rounded to the nearest whole index, clamped to a valid position.
    /// </summary>
    private static object MapAnswer(AskDecisionsQuestion question, AIDecisionAnswer answer) => answer switch
    {
        AIBinaryDecisionAnswer binary => new { Answer = binary.IsTrue(question.Threshold), Probability = binary.TrueProbability },
        AIChoiceDecisionAnswer choice => new { Choice = choice.Choice, Confidence = choice.Confidence },
        AIScoreDecisionAnswer score => new
        {
            Score = score.Score,
            Level = NearestLevelLabel(question.Levels, score.Score),
            Confidence = score.Confidence,
        },
        _ => throw new InvalidOperationException(
            $"Unexpected answer type '{answer.GetType().Name}' for question '{question.Alias}'."),
    };

    private static string NearestLevelLabel(IReadOnlyList<string>? levels, double score)
    {
        if (levels is null || levels.Count == 0)
        {
            return string.Empty;
        }

        var nearestIndex = Math.Clamp(
            (int)Math.Round(score, MidpointRounding.AwayFromZero),
            0,
            levels.Count - 1);

        return levels[nearestIndex];
    }
}
