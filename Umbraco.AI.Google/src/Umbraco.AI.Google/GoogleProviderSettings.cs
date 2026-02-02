using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Google;

/// <summary>
/// Settings for the Google provider.
/// </summary>
public class GoogleProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Google AI services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }
}
