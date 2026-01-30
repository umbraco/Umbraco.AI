namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Defines a grader that validates test outcomes against success criteria.
/// Multiple graders can be attached to a single test.
/// </summary>
public sealed class AiTestGrader
{
    /// <summary>
    /// The unique identifier of the grader.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The ID of the test this grader belongs to.
    /// </summary>
    public Guid TestId { get; internal set; }

    /// <summary>
    /// The type of grader (e.g., "exact-match", "llm-judge", "semantic-similarity").
    /// </summary>
    public required string GraderTypeId { get; set; }

    /// <summary>
    /// The name of this grader instance.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The grader-specific configuration as JSON.
    /// Structure depends on the grader type's ConfigType.
    /// </summary>
    public required string ConfigJson { get; set; }

    /// <summary>
    /// Whether to negate the grader result (fail becomes pass, pass becomes fail).
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// The severity level of this grader (Info, Warning, Error).
    /// </summary>
    public AiTestGraderSeverity Severity { get; set; } = AiTestGraderSeverity.Error;

    /// <summary>
    /// The weight of this grader for scoring purposes (0.0 to 1.0).
    /// Used when calculating aggregate scores across multiple graders.
    /// </summary>
    public float Weight { get; set; } = 1.0f;

    /// <summary>
    /// The sort order for displaying graders.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Severity levels for graders.
/// </summary>
public enum AiTestGraderSeverity
{
    /// <summary>
    /// Informational grader - failure doesn't affect test pass/fail status.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning grader - failure is noted but test can still pass.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error grader - failure causes test to fail.
    /// </summary>
    Error = 2
}
