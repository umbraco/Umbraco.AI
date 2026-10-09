namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// A single fact about a model, ready to display.
/// </summary>
/// <remarks>
/// This is a class with <c>init</c> properties, not a positional record, so optional properties can be added later
/// without breaking providers compiled against an earlier version.
/// </remarks>
public sealed class AIModelFact
{
    /// <summary>
    /// Gets the key that identifies this fact, for example <c>openrouter.contextWindow</c>.
    /// </summary>
    /// <remarks>
    /// Must be unique per fact. Prefix it with the owning package to avoid clashes.
    /// </remarks>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the label for this fact. Either plain text or a localization key starting with <c>#</c>
    /// (for example <c>#uaiModelFacts_contextWindow</c>), which the backoffice resolves.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets an optional shorter label, used where space is tight such as a column title.
    /// Falls back to <see cref="Label"/> when <c>null</c>. May be a <c>#</c> localization key.
    /// </summary>
    public string? ShortLabel { get; init; }

    /// <summary>
    /// Gets the display text for the value, already formatted by the provider.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets an optional numeric value used for sorting. <c>null</c> means the fact is not sortable.
    /// </summary>
    public double? SortValue { get; init; }

    /// <summary>
    /// Gets optional extra text, shown as a tooltip. May be a <c>#</c> localization key.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Gets how the fact should be emphasized. Defaults to <see cref="AIModelFactTone.Neutral"/>.
    /// </summary>
    public AIModelFactTone Tone { get; init; } = AIModelFactTone.Neutral;

    /// <summary>
    /// Gets an optional "learn more" link. Must be an absolute <c>http</c> or <c>https</c> URL;
    /// any other scheme is not rendered.
    /// </summary>
    public string? Url { get; init; }
}
