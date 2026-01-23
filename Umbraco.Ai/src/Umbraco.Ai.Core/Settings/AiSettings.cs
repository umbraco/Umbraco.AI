namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Represents AI settings configurable via the backoffice.
/// </summary>
public sealed class AiSettings
{
    /// <summary>
    /// The ID of the default profile to use for chat operations.
    /// </summary>
    public Guid? DefaultChatProfileId { get; set; }

    /// <summary>
    /// The ID of the default profile to use for embedding operations.
    /// </summary>
    public Guid? DefaultEmbeddingProfileId { get; set; }
}
