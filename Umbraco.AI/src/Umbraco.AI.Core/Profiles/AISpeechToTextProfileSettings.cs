namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Profile settings specific to the speech-to-text capability.
/// </summary>
public sealed class AISpeechToTextProfileSettings : IAIProfileSettings
{
    /// <summary>
    /// BCP-47 language hint for transcription (e.g., "en", "de", "ja").
    /// </summary>
    public string? Language { get; init; }
}
