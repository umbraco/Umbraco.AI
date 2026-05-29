using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Output produced by the <see cref="TranscribeAudioAction"/>.
/// </summary>
public sealed class TranscribeAudioOutput
{
    /// <summary>
    /// Gets the transcribed text of the audio file.
    /// </summary>
    [Field(Label = "Text", Description = "The transcribed text of the audio file.")]
    public string Text { get; init; } = string.Empty;
}
