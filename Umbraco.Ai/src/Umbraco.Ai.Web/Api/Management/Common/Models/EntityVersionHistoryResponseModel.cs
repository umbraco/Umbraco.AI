using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Common.Models;

/// <summary>
/// Response model for version history with pagination info.
/// </summary>
public class EntityVersionHistoryResponseModel
{
    /// <summary>
    /// The current version of the entity.
    /// </summary>
    [Required]
    public int CurrentVersion { get; set; }

    /// <summary>
    /// The total number of versions available.
    /// </summary>
    [Required]
    public int TotalVersions { get; set; }

    /// <summary>
    /// The list of version entries.
    /// </summary>
    [Required]
    public IEnumerable<EntityVersionResponseModel> Versions { get; set; } = [];
}
