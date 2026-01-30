using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Configuration for the LLM-as-judge grader.
/// </summary>
public class LLMJudgeGraderConfig
{
    /// <summary>
    /// The AI profile ID to use as the judge (must be a chat profile).
    /// </summary>
    [AiField(
        Label = "Judge Profile ID",
        Description = "The AI profile ID to use for judging the output (should be a capable chat model)",
        SortOrder = 1)]
    public Guid JudgeProfileId { get; set; }

    /// <summary>
    /// The grading rubric with criteria and instructions.
    /// </summary>
    [AiField(
        Label = "Rubric",
        Description = "Grading criteria and instructions for the judge. Be specific about what constitutes a good response.",
        SortOrder = 2)]
    public string Rubric { get; set; } = string.Empty;

    /// <summary>
    /// The minimum score (1-5) required to pass.
    /// </summary>
    [AiField(
        Label = "Passing Score",
        Description = "Minimum score (1-5) required to pass. 4 is recommended for high-quality outputs.",
        SortOrder = 3)]
    public int PassingScore { get; set; } = 4;

    /// <summary>
    /// Whether to include the full transcript for context.
    /// </summary>
    [AiField(
        Label = "Include Transcript",
        Description = "Provide the full conversation transcript to the judge for additional context",
        SortOrder = 4)]
    public bool IncludeTranscript { get; set; } = true;
}
