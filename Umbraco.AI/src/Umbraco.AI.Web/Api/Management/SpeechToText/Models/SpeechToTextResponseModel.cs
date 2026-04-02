namespace Umbraco.AI.Web.Api.Management.SpeechToText.Models;

/// <summary>
/// Response model for a speech-to-text transcription.
/// </summary>
public class SpeechToTextResponseModel
{
    /// <summary>
    /// The transcribed text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
