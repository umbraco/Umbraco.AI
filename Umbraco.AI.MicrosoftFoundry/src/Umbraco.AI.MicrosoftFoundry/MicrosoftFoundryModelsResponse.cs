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
/// <remarks>
/// Fills from either listing path, which report different amounts. The models API supplies only
/// <see cref="Id"/> (plus capabilities); the deployments API additionally reports what the deployment
/// fronts, which is carried in <see cref="ModelName"/>, <see cref="ModelVersion"/> and
/// <see cref="ModelPublisher"/>. Those three are <c>null</c> on the models API path, so every consumer has
/// to cope with not knowing — see <c>MicrosoftFoundryModelUtilities.SupportsSamplingParameters</c>.
/// </remarks>
internal sealed class MicrosoftFoundryModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("capabilities")]
    public MicrosoftFoundryModelCapabilities? Capabilities { get; set; }

    /// <summary>
    /// The underlying model a deployment fronts (e.g. <c>gpt-4o</c>), when known.
    /// </summary>
    /// <remarks>
    /// <see cref="Id"/> is the value passed to the API and stored on a profile. On the deployments path
    /// that is the deployment's own name, which a user chooses and which need not resemble the model —
    /// hence this being the name to reason about a model's behaviour from.
    /// </remarks>
    public string? ModelName { get; set; }

    /// <summary>
    /// The deployed model version (e.g. <c>2024-11-20</c>), when known.
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// The publisher of the underlying model (e.g. <c>OpenAI</c>), when known.
    /// </summary>
    public string? ModelPublisher { get; set; }
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
