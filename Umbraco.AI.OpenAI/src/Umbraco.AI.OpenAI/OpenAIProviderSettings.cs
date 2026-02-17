using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Settings for the OpenAI provider.
/// </summary>
public class OpenAIProviderSettings
{
    /// <summary>
    /// The API key for authenticating with OpenAI services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional organization ID for OpenAI API requests.
    /// </summary>
    [AIField]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.openai.com/v1";
}