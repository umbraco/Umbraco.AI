using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Moonshot;

/// <summary>
/// Settings for the Moonshot provider.
/// </summary>
public class MoonshotProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Moonshot services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.moonshot.ai/v1";
}
