using System.Text.Json.Serialization;

namespace Umbraco.Ai.MicrosoftFoundry;

/// <summary>
/// Response from the Microsoft AI Foundry Models List API.
/// </summary>
internal sealed class MicrosoftFoundryModelsResponse
{
    /// <summary>
    /// Gets or sets the list of models.
    /// </summary>
    [JsonPropertyName("data")]
    public List<MicrosoftFoundryModelInfo> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the object type (always "list").
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";
}

/// <summary>
/// Information about a Microsoft AI Foundry model.
/// </summary>
internal sealed class MicrosoftFoundryModelInfo
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type (always "model").
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "model";

    /// <summary>
    /// Gets or sets the model status.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle status.
    /// </summary>
    [JsonPropertyName("lifecycle_status")]
    public string? LifecycleStatus { get; set; }

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public MicrosoftFoundryModelCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }
}

/// <summary>
/// Model capabilities indicating what operations the model supports.
/// </summary>
internal sealed class MicrosoftFoundryModelCapabilities
{
    /// <summary>
    /// Gets or sets whether the model supports chat completion.
    /// </summary>
    [JsonPropertyName("chat_completion")]
    public bool ChatCompletion { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports completion (legacy).
    /// </summary>
    [JsonPropertyName("completion")]
    public bool Completion { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports embeddings.
    /// </summary>
    [JsonPropertyName("embeddings")]
    public bool Embeddings { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports fine-tuning.
    /// </summary>
    [JsonPropertyName("fine_tune")]
    public bool FineTune { get; set; }

    /// <summary>
    /// Gets or sets whether the model supports inference.
    /// </summary>
    [JsonPropertyName("inference")]
    public bool Inference { get; set; }
}
