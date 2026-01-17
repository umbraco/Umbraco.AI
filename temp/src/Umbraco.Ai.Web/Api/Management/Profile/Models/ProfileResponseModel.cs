using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Profile.Models;

/// <summary>
/// Response model for a profile.
/// </summary>
public class ProfileResponseModel
{
    /// <summary>
    /// The unique identifier of the profile.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the profile.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the profile.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The capability of the profile (e.g., Chat, Embedding).
    /// </summary>
    [Required]
    public string Capability { get; set; } = string.Empty;

    /// <summary>
    /// The model reference for this profile.
    /// </summary>
    public ModelRefModel? Model { get; set; }

    /// <summary>
    /// The ID of the connection to use for this profile.
    /// </summary>
    [Required]
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Capability-specific settings for the profile.
    /// </summary>
    public ProfileSettingsModel? Settings { get; set; }

    /// <summary>
    /// Tags associated with the profile.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];
}
