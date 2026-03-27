namespace Umbraco.AI.Core.Models;

/// <summary>
/// AI Capability Enum
/// </summary>
public enum AICapability
{
    /// <summary>
    /// Chat capability
    /// </summary>
    Chat = 0,
    
    /// <summary>
    /// Embedding generation capability
    /// </summary>
    Embedding = 1,
    
    /// <summary>
    /// Media generation capability
    /// </summary>
    Media = 2,
    
    /// <summary>
    /// Content moderation capability
    /// </summary>
    Moderation = 3,

    /// <summary>
    /// Speech-to-text transcription capability
    /// </summary>
    SpeechToText = 4

    // Future: TextToSpeech = 5, SpeechToSpeech = 6
}