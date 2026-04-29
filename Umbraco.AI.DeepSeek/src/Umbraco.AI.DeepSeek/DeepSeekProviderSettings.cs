using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.DeepSeek;

/// <summary>
/// Settings for the DeepSeek provider.
/// </summary>
public class DeepSeekProviderSettings
{
    /// <summary>
    /// The API key for authenticating with DeepSeek services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.deepseek.com";
}
