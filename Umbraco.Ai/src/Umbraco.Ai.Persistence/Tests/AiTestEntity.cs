namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// EF Core entity representing an AI test.
/// </summary>
internal class AiTestEntity
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the test (used for lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the test.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The test feature ID (e.g., "prompt", "agent", "custom").
    /// </summary>
    public string TestTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The target ID (GUID string or alias).
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Whether TargetId is an alias.
    /// </summary>
    public bool TargetIsAlias { get; set; }

    /// <summary>
    /// The test case configuration as JSON.
    /// </summary>
    public string TestCaseJson { get; set; } = string.Empty;

    /// <summary>
    /// Number of runs to execute (1 to N).
    /// </summary>
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// Tags serialized as comma-separated string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Whether this test is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The ID of the baseline run for comparison.
    /// </summary>
    public Guid? BaselineRunId { get; set; }

    /// <summary>
    /// Current version of the test.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the test was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// When the test was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The key (GUID) of the user who created this test.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this test.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Navigation property for graders.
    /// </summary>
    public ICollection<AiTestGraderEntity> Graders { get; set; } = new List<AiTestGraderEntity>();
}
