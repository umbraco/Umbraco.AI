using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Settings.Models;

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

    /// <summary>
    /// The ID of the profile to use for internal classification tasks (e.g., agent routing).
    /// </summary>
    public Guid? ClassifierChatProfileId { get; set; }

    /// <summary>
    /// The ID of the default profile to use for speech-to-text operations.
    /// </summary>
    public Guid? DefaultSpeechToTextProfileId { get; set; }

    /// <summary>
    /// The ID of the default profile to use for image-generation operations.
    /// </summary>
    public Guid? DefaultImageGenerationProfileId { get; set; }

    /// <summary>
    /// The ID of the default profile to use for decision operations.
    /// </summary>
    public Guid? DefaultDecisionProfileId { get; set; }

    /// <summary>
    /// How the AI-generated disclosure notice is shown (Always, Dismissible, Off).
    /// </summary>
    [Required]
    public string DisclosureNoticeMode { get; set; } = "Always";
}
