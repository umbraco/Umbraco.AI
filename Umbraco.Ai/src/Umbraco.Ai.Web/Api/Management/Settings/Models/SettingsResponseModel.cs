namespace Umbraco.Ai.Web.Api.Management.Settings.Models;

/// <summary>
/// Response model for AI settings.
/// </summary>
public class SettingsResponseModel
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
