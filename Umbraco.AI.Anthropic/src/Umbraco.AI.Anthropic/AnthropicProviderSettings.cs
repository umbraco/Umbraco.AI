using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Settings for the Anthropic provider.
/// </summary>
public class AnthropicProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Anthropic services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField(DefaultValue = "https://api.anthropic.com")]
    public string? Endpoint { get; set; }
}