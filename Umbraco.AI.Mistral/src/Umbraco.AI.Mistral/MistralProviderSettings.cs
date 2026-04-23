using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Mistral;

/// <summary>
/// Settings for the Mistral provider.
/// </summary>
public class MistralProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Mistral services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }
}
