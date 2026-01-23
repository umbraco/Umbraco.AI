using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Connection.Models;

/// <summary>
/// Lightweight response model for a connection item (used in lists).
/// </summary>
public class ConnectionItemResponseModel
{
    /// <summary>
    /// The unique identifier of the connection.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The unique alias for the connection (used for programmatic lookup).
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the connection.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the provider this connection is for.
    /// </summary>
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the connection was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the connection was created.
    /// </summary>
    public DateTime DateModified { get; set; }
}
