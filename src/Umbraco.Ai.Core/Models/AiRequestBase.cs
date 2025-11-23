namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Base class for AI requests.
/// </summary>
public abstract class AiRequestBase
{
    /// <summary>
    /// The AI model to use for text generation (power users only).
    /// </summary>
    public AiModelRef? Model { get; init; }
    
    /// <summary>
    /// The name of an explicit profile to use for this request (otherwise uses the default).
    /// </summary>
    public string? ProfileName { get; init; }
    
    /// <summary>
    /// The temperature setting for the AI model, influencing randomness in responses.
    /// </summary>
    public float? Temperature { get; init; }
    
    /// <summary>
    /// The maximum number of tokens the AI model can generate in a single response.
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// Additional metadata for the request.
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}