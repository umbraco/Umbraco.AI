using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Common.Models;

/// <summary>
/// Response model for a single version history entry.
/// </summary>
public class EntityVersionResponseModel
{
    /// <summary>
    /// The unique identifier of this version record.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the entity this version belongs to.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// The version number (1, 2, 3, etc.).
    /// </summary>
    [Required]
    public int Version { get; set; }

    /// <summary>
    /// The date and time when this version record was created, i.e. when the version was replaced and archived.
    /// For the current version, the date it was last modified.
    /// </summary>
    [Required]
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The user key (GUID) of the user whose save archived this version.
    /// For the current version, the user who last modified it.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The display name of the user who created this version.
    /// </summary>
    public string? CreatedByUserName { get; set; }

    /// <summary>
    /// The date and time when the entity state in this version was made.
    /// </summary>
    [Required]
    public DateTime DateAuthored { get; set; }

    /// <summary>
    /// The user key (GUID) of the user who made the entity state in this version.
    /// </summary>
    public Guid? AuthoredByUserId { get; set; }

    /// <summary>
    /// Optional description of what changed in this version.
    /// </summary>
    public string? ChangeDescription { get; set; }
}
