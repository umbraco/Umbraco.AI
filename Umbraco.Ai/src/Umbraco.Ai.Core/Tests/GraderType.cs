namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Defines the types of graders available for test evaluation.
/// </summary>
public enum GraderType
{
    /// <summary>
    /// Code-based grader - deterministic, fast evaluation using rules and patterns.
    /// Examples: ExactMatch, Contains, Regex, JSONSchema, ToolCall verification.
    /// </summary>
    CodeBased = 0,

    /// <summary>
    /// Model-based grader - uses AI models for flexible, subjective evaluation.
    /// Examples: LLM-as-judge with rubrics, semantic similarity scoring.
    /// </summary>
    ModelBased = 1,

    /// <summary>
    /// Human grader - requires manual review (future feature).
    /// Provides human-in-the-loop evaluation for complex or subjective criteria.
    /// </summary>
    Human = 2
}
