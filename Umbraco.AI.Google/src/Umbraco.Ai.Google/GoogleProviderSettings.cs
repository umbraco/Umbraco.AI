using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Google;

/// <summary>
/// Settings for the Google provider.
/// </summary>
public class GoogleProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Google AI services.
    /// </summary>
    [AiField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }
}
