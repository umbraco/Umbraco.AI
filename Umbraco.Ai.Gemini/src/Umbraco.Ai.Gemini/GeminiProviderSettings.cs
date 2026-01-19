using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Gemini;

/// <summary>
/// Settings for the Google Gemini provider.
/// </summary>
public class GeminiProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Google Gemini services.
    /// </summary>
    [AiField]
    [Required]
    public string? ApiKey { get; set; }
}
