using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.MicrosoftFoundry;

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
    [AiField]
    [Required]
    public string? Endpoint { get; set; }

    /// <summary>
    /// The API key for authenticating with Microsoft AI Foundry services.
    /// </summary>
    [AiField]
    [Required]
    public string? ApiKey { get; set; }
}
