using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Connection.Models;

/// <summary>
/// Response model for a connection.
/// </summary>
public class ConnectionResponseModel
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
    /// Provider-specific settings (credentials, endpoints, etc.).
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the connection was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// When the connection was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The current version of the connection.
    /// </summary>
    public int Version { get; set; }
}