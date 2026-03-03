using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Ollama;

/// <summary>
/// Settings for the Ollama provider.
/// </summary>
public class OllamaProviderSettings
{
    /// <summary>
    /// The Ollama API endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Optional API key for authenticating with remote Ollama instances.
    /// </summary>
    [AIField(IsSensitive = true)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional custom headers for authentication (e.g., Bearer tokens).
    /// Format: "Header-Name: Header-Value" (one per line).
    /// </summary>
    [AIField]
    public string? CustomHeaders { get; set; }
}
