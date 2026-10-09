using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// Settings for the TypeSafe AI provider.
/// </summary>
public class TypeSafeProviderSettings
{
    /// <summary>
    /// The API key for authenticating with TypeSafe AI (Jev) services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the TypeSafe AI API.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://api.typesafe.ai";
}
