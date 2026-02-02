using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.OpenAi;

/// <summary>
/// Settings for the OpenAI provider.
/// </summary>
public class OpenAiProviderSettings
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
    [AIField(DefaultValue = "https://api.openai.com/v1")]
    public string? Endpoint { get; set; }
}