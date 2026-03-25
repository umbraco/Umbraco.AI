namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Estimates token count using a word-based heuristic.
/// Uses a 1.3x multiplier on word count, which is a conservative approximation
/// for most embedding models. Can be replaced via DI with a model-specific
/// tokenizer for higher accuracy.
/// </summary>
public sealed class WordBasedAITokenCounter : IAITokenCounter
{
    private const double TokensPerWord = 1.3;

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var wordCount = 0;
        var inWord = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                wordCount++;
            }
        }

        return (int)Math.Ceiling(wordCount * TokensPerWord);
    }
}
