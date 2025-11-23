namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents the usage statistics of an AI model interaction, including token counts and provider-specific usage data.
/// </summary>
public sealed class AiUsage
{
    /// <summary>
    /// The number of tokens used in the prompt.
    /// </summary>
    public int PromptTokens { get; init; }
    
    /// <summary>
    /// The number of tokens generated in the completion.
    /// </summary>
    public int CompletionTokens { get; init; }
    
    /// <summary>
    /// The total number of tokens used (prompt + completion).
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
    
    /// <summary>
    /// Provider-specific usage data.
    /// </summary>
    public IDictionary<string, object> ProviderUsage { get; init; } = new Dictionary<string, object>();
}