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
    SpeechToText = 4,

    /// <summary>
    /// Image generation capability (text-to-image and image editing).
    /// </summary>
    /// <remarks>
    /// Experimental — gated behind the <c>Umbraco:AI:Experimental:ImageGeneration</c> feature flag
    /// (default off). Distinct from the reserved <see cref="Media"/> slot.
    /// </remarks>
    ImageGeneration = 5

    // Future: TextToSpeech = 6, SpeechToSpeech = 7
}