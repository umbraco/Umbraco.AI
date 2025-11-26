using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Provider.Models;

/// <summary>
/// Lightweight response model for a provider (used in lists).
/// </summary>
public class ProviderItemResponseModel
{
    /// <summary>
    /// The unique identifier of the provider.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the provider.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The capabilities supported by this provider.
    /// </summary>
    [Required]
    public IEnumerable<string> Capabilities { get; set; } = [];
}
