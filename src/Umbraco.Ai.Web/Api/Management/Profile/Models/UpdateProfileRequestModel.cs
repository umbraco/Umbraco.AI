using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Profile.Models;

/// <summary>
/// Request model for updating an existing profile.
/// </summary>
public class UpdateProfileRequestModel
{
    /// <summary>
    /// The unique alias for the profile.
    /// </summary>
    [Required]
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the profile.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// The model reference for this profile.
    /// </summary>
    [Required]
    public required ModelRefModel Model { get; init; }

    /// <summary>
    /// The ID of the connection to use for this profile.
    /// </summary>
    [Required]
    public required Guid ConnectionId { get; init; }

    /// <summary>
    /// Capability-specific settings for the profile.
    /// </summary>
    public ProfileSettingsModel? Settings { get; init; }

    /// <summary>
    /// Tags associated with the profile.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
