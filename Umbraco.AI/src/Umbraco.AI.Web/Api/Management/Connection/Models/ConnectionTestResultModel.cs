using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Connection.Models;

/// <summary>
/// Response model for a connection test result.
/// </summary>
public class ConnectionTestResultModel
{
    /// <summary>
    /// Whether the connection test was successful.
    /// </summary>
    [Required]
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
