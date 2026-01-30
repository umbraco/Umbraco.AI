using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Tests.Common.Fakes;

/// <summary>
/// Fake settings class for use in tests.
/// </summary>
public class FakeProviderSettings
{
    [AiField(Label = "API Key", Description = "Enter your API key")]
    [Required(ErrorMessage = "API Key is required")]
    public string? ApiKey { get; set; }

    [AiField(Label = "Base URL", Description = "The base URL for the API")]
    public string? BaseUrl { get; set; }

    [AiField(Label = "Max Retries", Description = "Maximum retry attempts")]
    public int MaxRetries { get; set; } = 3;

    [AiField(Label = "Enabled", Description = "Enable this feature")]
    public bool Enabled { get; set; } = true;
}
