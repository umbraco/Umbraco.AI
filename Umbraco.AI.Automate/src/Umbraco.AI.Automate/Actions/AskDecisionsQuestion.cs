namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// One question in <see cref="AskDecisionsSettings.Questions"/>, edited through the
/// <c>Uai.PropertyEditorUi.DecisionQuestionList</c> property editor. Flat, not polymorphic
/// (ARCHITECTURE decision 6) — fields irrelevant to <see cref="Kind"/> are simply left unset.
/// Property names map one-for-one to the editor's row shape (<c>{ kind, alias, instructions,
/// trueCriteria, falseCriteria, threshold, options, levels }</c>) and round-trip through
/// <c>EditableModelResolver</c>'s camelCase JSON options without any explicit
/// <c>JsonPropertyName</c> attributes.
/// </summary>
public sealed class AskDecisionsQuestion
{
    /// <summary>
    /// Gets or sets the question kind: <c>"binary"</c>, <c>"choice"</c>, or <c>"score"</c>,
    /// matching the editor's <c>kind</c> values. Any other value is rejected as invalid
    /// settings before any provider call.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question's output key. Required, unique within
    /// <see cref="AskDecisionsSettings.Questions"/>, must start with a letter and contain only
    /// letters, digits, or underscores — enforced by <see cref="AskDecisionsAction"/> before any
    /// provider call. Also used as the question's <see cref="Umbraco.AI.Core.Decision.AIDecisionQuestion.Id"/>.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Gets or sets what to decide — the natural-language instructions given to the model.</summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what "yes" means, when it needs spelling out beyond <see cref="Instructions"/>.
    /// Binary questions only.
    /// </summary>
    public string? TrueCriteria { get; set; }

    /// <summary>
    /// Gets or sets what "no" means, when it needs spelling out beyond <see cref="Instructions"/>.
    /// Binary questions only.
    /// </summary>
    public string? FalseCriteria { get; set; }

    /// <summary>
    /// Gets or sets the minimum probability, from 0.0 to 1.0, counted as "yes". Binary questions
    /// only; defaults to 0.5 when the editor leaves the field unset.
    /// </summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the options to choose from. Choice questions only. Must contain 2 to 255
    /// entries with unique keys — enforced downstream by Core's Decision validator.
    /// </summary>
    public List<AskChoiceDecisionOption>? Options { get; set; }

    /// <summary>
    /// Gets or sets the score's levels, lowest first. Score questions only. Must contain 2 to 10
    /// non-blank entries — enforced downstream by Core's Decision validator.
    /// </summary>
    public List<string>? Levels { get; set; }
}
