namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines success criteria for a test - validates test outcomes.
/// Multiple graders can be applied to evaluate different aspects (format, content quality, tool usage, etc.).
/// </summary>
public sealed class AITestGraderConfig
{
    /// <summary>
    /// Unique identifier for the grader within the test.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the grader type (implementation) to use.
    /// References an IAITestGrader implementation (e.g., "exact-match", "llm-judge").
    /// </summary>
    public required string GraderTypeId { get; set; }

    /// <summary>
    /// Name of this grader instance.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Grader-specific configuration as JSON.
    /// Structure depends on the grader type's ConfigType.
    /// </summary>
    public string? ConfigJson { get; set; }

    /// <summary>
    /// Whether to negate the grader result (pass becomes fail, fail becomes pass).
    /// Useful for assertions like "should NOT contain X".
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// Severity level of this grader.
    /// - Info: Provides information but doesn't affect pass/fail
    /// - Warning: Fails the test but doesn't block
    /// - Error: Fails the test and blocks (default)
    /// </summary>
    public AITestGraderSeverity Severity { get; set; } = AITestGraderSeverity.Error;

    /// <summary>
    /// Weight for scoring (0-1).
    /// Used when calculating aggregate scores across multiple graders.
    /// Default is 1.0 (equal weight).
    /// </summary>
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Severity level for grader results.
/// </summary>
public enum AITestGraderSeverity
{
    /// <summary>
    /// Informational only - doesn't affect test pass/fail.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning - logs failure but doesn't block test.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error - fails the test and blocks.
    /// </summary>
    Error = 2
}
