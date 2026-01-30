namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core entity representing a test grader.
/// </summary>
internal class AiTestGraderEntity
{
    /// <summary>
    /// Unique identifier for the grader.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the test.
    /// </summary>
    public Guid TestId { get; set; }

    /// <summary>
    /// The grader type ID (e.g., "exact-match", "llm-judge").
    /// </summary>
    public string GraderTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this grader instance.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this grader validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Grader-specific configuration as JSON.
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether to negate the result.
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// Severity level (0=Info, 1=Warning, 2=Error).
    /// </summary>
    public int Severity { get; set; } = 2;

    /// <summary>
    /// Weight for scoring (0.0 to 1.0).
    /// </summary>
    public float Weight { get; set; } = 1.0f;

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to test.
    /// </summary>
    public AiTestEntity Test { get; set; } = null!;
}
