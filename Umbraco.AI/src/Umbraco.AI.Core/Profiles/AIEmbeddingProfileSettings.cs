namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Settings specific to Embedding capability profiles.
/// </summary>
public sealed class AIEmbeddingProfileSettings : IAIProfileSettings
{
    /// <summary>
    /// The number of dimensions for the generated embeddings.
    /// When null, the model's default dimension count is used.
    /// </summary>
    public int? Dimensions { get; init; }
}
