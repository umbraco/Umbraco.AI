using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="TranscribeAudioAction"/>.
/// </summary>
public sealed class TranscribeAudioSettings
{
    /// <summary>
    /// Gets or sets the audio source. Accepts a media GUID, a media picker value, or a path.
    /// Supports binding syntax so the value can be produced by an upstream action.
    /// </summary>
    [Field(Label = "Audio source",
        Description = "Path to the audio file, a media GUID, or a media picker value. Supports binding syntax.",
        SupportsBindings = true)]
    public string AudioPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the speech-to-text profile to use. When empty, the default
    /// speech-to-text profile is used.
    /// </summary>
    [Field(Label = "Profile",
        Description = "The speech-to-text profile to use. Leave empty to use the default profile.",
        SortOrder = 1,
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = """[{ "alias": "capability", "value": "SpeechToText" }]""")]
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets an optional BCP-47 language hint (e.g. <c>en</c>, <c>da-DK</c>).
    /// Leave empty for auto-detect.
    /// </summary>
    [Field(Label = "Language hint",
        Description = "Optional BCP-47 language code (e.g. 'en', 'da-DK'). Leave empty for auto-detect.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string? Language { get; set; }
}
