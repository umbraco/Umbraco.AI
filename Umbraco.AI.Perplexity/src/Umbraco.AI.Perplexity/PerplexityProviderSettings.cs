using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Perplexity;

/// <summary>
/// Settings for the Perplexity provider.
/// </summary>
public class PerplexityProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Perplexity services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.perplexity.ai";
}
