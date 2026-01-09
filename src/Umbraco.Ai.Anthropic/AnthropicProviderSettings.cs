using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Anthropic;

/// <summary>
/// Settings for the Anthropic provider.
/// </summary>
public class AnthropicProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Anthropic services.
    /// </summary>
    [AiField]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AiField(DefaultValue = "https://api.anthropic.com")]
    public string? Endpoint { get; set; }
}