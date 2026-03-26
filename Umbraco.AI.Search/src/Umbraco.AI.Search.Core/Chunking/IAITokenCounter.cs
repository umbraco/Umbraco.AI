namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Counts the number of tokens in a text string.
/// </summary>
public interface IAITokenCounter
{
    /// <summary>
    /// Returns the estimated number of tokens in the given text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <returns>The estimated token count.</returns>
    int CountTokens(string text);
}
