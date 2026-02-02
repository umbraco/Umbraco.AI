using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Settings for the Microsoft AI Foundry provider.
/// </summary>
public class MicrosoftFoundryProviderSettings
{
    /// <summary>
    /// The Microsoft AI Foundry endpoint URL.
    /// </summary>
    /// <remarks>
    /// Example: https://your-resource.services.ai.azure.com/
    /// </remarks>
    [AIField]
    [Required]
    public string? Endpoint { get; set; }

    /// <summary>
    /// The API key for authenticating with Microsoft AI Foundry services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }
}
