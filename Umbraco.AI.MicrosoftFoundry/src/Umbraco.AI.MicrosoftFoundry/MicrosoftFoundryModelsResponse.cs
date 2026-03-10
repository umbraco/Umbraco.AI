using System.Text.Json.Serialization;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Response from the Microsoft AI Foundry Models List API.
/// </summary>
internal sealed class MicrosoftFoundryModelsResponse
{
    [JsonPropertyName("data")]
    public List<MicrosoftFoundryModelInfo> Data { get; set; } = [];
}

/// <summary>
/// Information about a Microsoft AI Foundry model.
/// </summary>
internal sealed class MicrosoftFoundryModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("capabilities")]
    public MicrosoftFoundryModelCapabilities? Capabilities { get; set; }
}

/// <summary>
/// Model capabilities indicating what operations the model supports.
/// </summary>
internal sealed class MicrosoftFoundryModelCapabilities
{
    [JsonPropertyName("chat_completion")]
    public bool ChatCompletion { get; set; }

    [JsonPropertyName("embeddings")]
    public bool Embeddings { get; set; }
}
