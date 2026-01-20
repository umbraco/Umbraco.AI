using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// A flexible context item that can contain any data.
/// Matches the AG-UI protocol's simple structure.
/// </summary>
public class RequestContextItemModel
{
    /// <summary>
    /// Human-readable description (e.g., "Currently editing document: My Page").
    /// </summary>
    [Required]
    public required string Description { get; init; }

    /// <summary>
    /// The context data - can be anything (JSON serializable).
    /// </summary>
    public string? Value { get; init; }
}
