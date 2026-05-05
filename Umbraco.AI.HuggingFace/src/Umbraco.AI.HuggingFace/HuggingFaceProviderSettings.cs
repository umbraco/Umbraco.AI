using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.HuggingFace;

/// <summary>
/// Settings for the Hugging Face provider.
/// </summary>
public class HuggingFaceProviderSettings
{
    /// <summary>
    /// The Hugging Face access token used to authenticate against the Inference Providers router.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Endpoint for the Hugging Face Inference Providers OpenAI-compatible router.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://router.huggingface.co/v1";
}
