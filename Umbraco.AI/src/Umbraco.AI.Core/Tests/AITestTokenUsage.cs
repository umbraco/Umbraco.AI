namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Token usage statistics for a test execution.
/// </summary>
public sealed class AITestTokenUsage
{
    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public int TotalTokens { get; set; }
}
