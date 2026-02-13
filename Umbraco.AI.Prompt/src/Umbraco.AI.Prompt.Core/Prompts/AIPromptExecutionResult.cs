using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Result of prompt execution.
/// </summary>
public class AIPromptExecutionResult
{
    /// <summary>
    /// The generated response content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>
    /// Available result options. Always present, may be empty.
    /// - Empty array: Informational only (OptionCount = 0)
    /// - Single item: One value to insert (OptionCount = 1)
    /// - Multiple items: User selects one (OptionCount >= 2)
    /// </summary>
    public required IReadOnlyList<AIPromptResultOption> ResultOptions { get; init; }

    /// <summary>
    /// Represents a single result option that can be displayed and optionally applied.
    /// </summary>
    public class AIPromptResultOption
    {
        /// <summary>
        /// Short label/title for the option (e.g., "Formal Tone", "Result").
        /// </summary>
        public required string Label { get; init; }

        /// <summary>
        /// The display value to show in the UI.
        /// </summary>
        public required string DisplayValue { get; init; }

        /// <summary>
        /// Optional description explaining this option.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// The property change to apply when this option is selected.
        /// Null for informational-only options.
        /// </summary>
        public AIPropertyChange? PropertyChange { get; init; }
    }
}
