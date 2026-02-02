using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake settings class for use in tests.
/// </summary>
public class FakeProviderSettings
{
    [AIField(Label = "API Key", Description = "Enter your API key")]
    [Required(ErrorMessage = "API Key is required")]
    public string? ApiKey { get; set; }

    [AIField(Label = "Base URL", Description = "The base URL for the API")]
    public string? BaseUrl { get; set; }

    [AIField(Label = "Max Retries", Description = "Maximum retry attempts")]
    public int MaxRetries { get; set; } = 3;

    [AIField(Label = "Enabled", Description = "Enable this feature")]
    public bool Enabled { get; set; } = true;
}
