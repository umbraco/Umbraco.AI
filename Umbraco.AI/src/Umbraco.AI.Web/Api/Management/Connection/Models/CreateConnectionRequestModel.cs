using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Connection.Models;

/// <summary>
/// Request model for creating a new connection.
/// </summary>
public class CreateConnectionRequestModel
{
    /// <summary>
    /// Unique alias for the connection (used for programmatic lookup).
    /// </summary>
    [Required]
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the connection.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// The ID of the provider this connection is for.
    /// </summary>
    [Required]
    public required string ProviderId { get; init; }

    /// <summary>
    /// Provider-specific settings (credentials, endpoints, etc.).
    /// </summary>
    public object? Settings { get; init; }

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
