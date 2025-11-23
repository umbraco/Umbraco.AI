namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Base class for AI response models.
/// </summary>
public abstract class AiResponseBase
{
    /// <summary>
    /// The name of the profile used for this AI operation, if applicable.
    /// </summary>
    public string? ProfileName { get; init; }
    
    /// <summary>
    /// The AI model reference used for this operation, if applicable.
    /// </summary>
    public AiModelRef? Model { get; init; }

    /// <summary>
    /// Usage statistics for this AI operation, if available.
    /// </summary>
    public AiUsage? Usage { get; init; }
    
    /// <summary>
    /// The raw response from the AI provider, if available.
    /// </summary>
    public object? RawResponse { get; init; }
}