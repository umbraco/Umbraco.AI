namespace Umbraco.Ai.Core.Models;

/// <summary>
/// AI Capability Enum
/// </summary>
public enum AiCapability
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
    Moderation = 3
}