using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Filter for querying usage analytics statistics.
/// </summary>
public sealed class AIUsageFilter
{
    /// <summary>
    /// Gets or sets the provider ID to filter by.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets or sets the model ID to filter by.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Gets or sets the profile ID to filter by.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Gets or sets the capability to filter by.
    /// </summary>
    public AICapability? Capability { get; init; }

    /// <summary>
    /// Gets or sets the user ID to filter by.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the entity type to filter by.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets or sets the feature type to filter by.
    /// </summary>
    public string? FeatureType { get; init; }
}
