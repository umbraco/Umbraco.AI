namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Configuration options for automated version history cleanup.
/// </summary>
/// <remarks>
/// <para>
/// When both <see cref="MaxVersionsPerEntity"/> and <see cref="RetentionDays"/> are set,
/// versions must satisfy BOTH conditions to be retained (AND logic).
/// </para>
/// <para>
/// Set <see cref="MaxVersionsPerEntity"/> or <see cref="RetentionDays"/> to 0 to disable
/// that specific cleanup type.
/// </para>
/// </remarks>
public sealed class AiVersionCleanupPolicy
{
    /// <summary>
    /// Gets or sets a value indicating whether version cleanup is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of versions to retain per entity.
    /// Older versions beyond this limit will be deleted.
    /// Set to 0 to disable count-based cleanup.
    /// Default is 50.
    /// </summary>
    public int MaxVersionsPerEntity { get; set; } = 50;

    /// <summary>
    /// Gets or sets the number of days to retain version history.
    /// Versions older than this will be deleted.
    /// Set to 0 to disable age-based cleanup.
    /// Default is 90 days.
    /// </summary>
    public int RetentionDays { get; set; } = 90;
}
