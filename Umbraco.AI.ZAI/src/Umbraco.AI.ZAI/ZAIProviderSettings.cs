using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.ZAI;

/// <summary>
/// Settings for the Z.AI provider.
/// </summary>
public class ZAIProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Z.AI services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.z.ai/api/paas/v4";
}
