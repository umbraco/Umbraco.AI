using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// Settings for the Fireworks AI provider.
/// </summary>
public class FireworksAIProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Fireworks AI services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// The Fireworks account whose model catalog is exposed.
    /// Defaults to <c>fireworks</c>, the public catalog of official models.
    /// Set to your own account id to expose fine-tuned or private models.
    /// </summary>
    [AIField]
    public string? AccountId { get; set; } = "fireworks";

    /// <summary>
    /// Base URL for the Fireworks AI OpenAI-compatible endpoint.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.fireworks.ai/inference/v1";
}
