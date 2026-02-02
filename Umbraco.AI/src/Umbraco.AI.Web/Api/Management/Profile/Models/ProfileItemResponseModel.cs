using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Profile.Models;

/// <summary>
/// Lightweight response model for a profile item (used in lists).
/// </summary>
public class ProfileItemResponseModel
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
    /// The date and time (in UTC) when the connection was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the connection was created.
    /// </summary>
    public DateTime DateModified { get; set; }
}
