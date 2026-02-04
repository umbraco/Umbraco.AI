namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core entity representing an AI test configuration.
/// </summary>
internal class AITestEntity
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
    /// Optional description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the test feature (harness) to use.
    /// </summary>
    public string TestTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Target identifier (Guid or alias).
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Whether TargetId is an alias (true) or Guid (false).
    /// </summary>
    public bool TargetIsAlias { get; set; }

    /// <summary>
    /// Test case configuration as JSON.
    /// </summary>
    public string TestCaseJson { get; set; } = string.Empty;

    /// <summary>
    /// Graders serialized as JSON array.
    /// </summary>
    public string? GradersJson { get; set; }

    /// <summary>
    /// Number of times to run this test.
    /// </summary>
    public int RunCount { get; set; } = 1;

    /// <summary>
    /// Tags array serialized as a comma-separated string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Whether this test is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional baseline run ID for regression detection.
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
}
