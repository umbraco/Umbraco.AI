using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// Settings for the Cloudflare provider.
/// </summary>
public class CloudflareProviderSettings
{
    /// <summary>
    /// The Cloudflare account the Workers AI models run under.
    /// </summary>
    [AIField]
    [Required]
    public string? AccountId { get; set; }

    /// <summary>
    /// The API token for authenticating with Cloudflare.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiToken { get; set; }

    /// <summary>
    /// Base URL for the Cloudflare API.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.cloudflare.com/client/v4";
}
