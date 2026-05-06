using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.TogetherAI;

/// <summary>
/// Settings for the Together AI provider.
/// </summary>
public class TogetherAIProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Together AI services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL. Defaults to the public Together AI endpoint.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.together.xyz/v1";
}
