namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Result of a version cleanup operation.
/// </summary>
public sealed class AiVersionCleanupResult
{
    /// <summary>
    /// Gets the number of versions deleted due to exceeding the age threshold.
    /// </summary>
    public int DeletedByAge { get; init; }

    /// <summary>
    /// Gets the number of versions deleted due to exceeding the count limit per entity.
    /// </summary>
    public int DeletedByCount { get; init; }

    /// <summary>
    /// Gets the total number of versions deleted.
    /// </summary>
    public int TotalDeleted => DeletedByAge + DeletedByCount;

    /// <summary>
    /// Gets the number of version records remaining after cleanup.
    /// </summary>
    public int RemainingVersions { get; init; }

    /// <summary>
    /// Gets a value indicating whether the cleanup was skipped.
    /// </summary>
    public bool WasSkipped { get; init; }

    /// <summary>
    /// Gets the reason why cleanup was skipped, if applicable.
    /// </summary>
    public string? SkipReason { get; init; }
}
