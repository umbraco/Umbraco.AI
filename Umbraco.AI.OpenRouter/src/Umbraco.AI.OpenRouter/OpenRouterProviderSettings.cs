using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.OpenRouter;

/// <summary>
/// Settings for the OpenRouter provider.
/// </summary>
public class OpenRouterProviderSettings
{
    /// <summary>
    /// The API key for authenticating with OpenRouter.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the OpenRouter OpenAI-compatible endpoint.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://openrouter.ai/api/v1";
}
