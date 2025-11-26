using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Connection.Models;

/// <summary>
/// Request model for updating an existing connection.
/// </summary>
public class UpdateConnectionRequestModel
{
    /// <summary>
    /// The display name of the connection.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Provider-specific settings (credentials, endpoints, etc.).
    /// </summary>
    public object? Settings { get; init; }

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; init; }
}
