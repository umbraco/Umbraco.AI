namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Splits text into chunks using a recursive character splitting strategy.
/// Tries to split on paragraph boundaries first, then sentences, then words.
/// </summary>
internal sealed class RecursiveAITextChunker : IAITextChunker
{
    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    private readonly IAITokenCounter _tokenCounter;

    public RecursiveAITextChunker(IAITokenCounter tokenCounter)
    {
        _tokenCounter = tokenCounter;
    }

    /// <inheritdoc />
    public IReadOnlyList<AITextChunk> ChunkText(string text, AITextChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ChunkOverlap >= options.MaxChunkSize)
        {
            throw new ArgumentException(
                "Chunk overlap must be less than the maximum chunk size.",
                nameof(options));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var segments = SplitRecursively(text, options.MaxChunkSize, separatorIndex: 0);
        return MergeWithOverlap(segments, text, options);
    }

    private List<TextSegment> SplitRecursively(string text, int maxTokens, int separatorIndex)
    {
        if (_tokenCounter.CountTokens(text) <= maxTokens)
        {
            return [new TextSegment(text, 0)];
        }

        // Try each separator in order of preference
        for (var i = separatorIndex; i < Separators.Length; i++)
        {
            var separator = Separators[i];
            var parts = SplitOnSeparator(text, separator);

            if (parts.Count <= 1)
            {
                continue;
            }

            var result = new List<TextSegment>();

            foreach (var part in parts)
            {
                if (_tokenCounter.CountTokens(part.Text) <= maxTokens)
                {
                    result.Add(part);
                }
                else
                {
                    // Recursively split with the next separator
                    var subSegments = SplitRecursively(part.Text, maxTokens, i + 1);
                    foreach (var sub in subSegments)
                    {
                        result.Add(new TextSegment(sub.Text, part.Offset + sub.Offset));
                    }
                }
            }

            if (result.Count > 1)
            {
                return result;
            }
        }

        // Last resort: force-split by character chunks
        return ForceSplit(text, maxTokens);
    }

    private static List<TextSegment> SplitOnSeparator(string text, string separator)
    {
        var result = new List<TextSegment>();
        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var sepIndex = text.IndexOf(separator, startIndex, StringComparison.Ordinal);

            if (sepIndex < 0)
            {
                var remaining = text[startIndex..].Trim();
                if (remaining.Length > 0)
                {
                    result.Add(new TextSegment(remaining, startIndex));
                }

                break;
            }

            // Include the separator with the preceding segment (except for ". " where we keep the period)
            var endIndex = separator == ". " ? sepIndex + 1 : sepIndex;
            var segment = text[startIndex..endIndex].Trim();

            if (segment.Length > 0)
            {
                result.Add(new TextSegment(segment, startIndex));
            }

            startIndex = sepIndex + separator.Length;
        }

        return result;
    }

    private List<TextSegment> ForceSplit(string text, int maxTokens)
    {
        var result = new List<TextSegment>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new List<string>();
        var currentOffset = 0;
        var chunkStartOffset = 0;

        foreach (var word in words)
        {
            currentChunk.Add(word);
            var candidate = string.Join(' ', currentChunk);

            if (_tokenCounter.CountTokens(candidate) > maxTokens && currentChunk.Count > 1)
            {
                // Remove last word and emit chunk
                currentChunk.RemoveAt(currentChunk.Count - 1);
                var chunkText = string.Join(' ', currentChunk);
                result.Add(new TextSegment(chunkText, chunkStartOffset));

                // Start new chunk with the word that didn't fit
                chunkStartOffset = currentOffset;
                currentChunk.Clear();
                currentChunk.Add(word);
            }

            currentOffset += word.Length + 1; // +1 for space
        }

        if (currentChunk.Count > 0)
        {
            result.Add(new TextSegment(string.Join(' ', currentChunk), chunkStartOffset));
        }

        return result;
    }

    private IReadOnlyList<AITextChunk> MergeWithOverlap(
        List<TextSegment> segments,
        string originalText,
        AITextChunkingOptions options)
    {
        if (segments.Count == 0)
        {
            return [];
        }

        // Merge small adjacent segments to fill chunks closer to max size
        var merged = MergeSmallSegments(segments, options.MaxChunkSize);

        if (merged.Count <= 1 || options.ChunkOverlap <= 0)
        {
            return merged
                .Select((s, i) => new AITextChunk(s.Text, i, s.Offset, s.Text.Length))
                .ToList();
        }

        // Apply overlap by prepending trailing text from the previous chunk
        var result = new List<AITextChunk>
        {
            new(merged[0].Text, 0, merged[0].Offset, merged[0].Text.Length),
        };

        for (var i = 1; i < merged.Count; i++)
        {
            var overlapText = GetOverlapText(merged[i - 1].Text, options.ChunkOverlap);
            var chunkText = overlapText.Length > 0
                ? overlapText + " " + merged[i].Text
                : merged[i].Text;

            // Trim if overlap caused us to exceed max size
            if (_tokenCounter.CountTokens(chunkText) > options.MaxChunkSize)
            {
                chunkText = merged[i].Text;
            }

            result.Add(new AITextChunk(chunkText, i, merged[i].Offset, merged[i].Text.Length));
        }

        return result;
    }

    private List<TextSegment> MergeSmallSegments(List<TextSegment> segments, int maxTokens)
    {
        var result = new List<TextSegment>();
        var currentText = string.Empty;
        var currentOffset = -1;

        foreach (var segment in segments)
        {
            if (currentText.Length == 0)
            {
                currentText = segment.Text;
                currentOffset = segment.Offset;
                continue;
            }

            var merged = currentText + " " + segment.Text;

            if (_tokenCounter.CountTokens(merged) <= maxTokens)
            {
                currentText = merged;
            }
            else
            {
                result.Add(new TextSegment(currentText, currentOffset));
                currentText = segment.Text;
                currentOffset = segment.Offset;
            }
        }

        if (currentText.Length > 0)
        {
            result.Add(new TextSegment(currentText, currentOffset));
        }

        return result;
    }

    private string GetOverlapText(string previousChunkText, int overlapTokens)
    {
        if (overlapTokens <= 0)
        {
            return string.Empty;
        }

        // Take trailing words from previous chunk until we reach the overlap token budget
        var words = previousChunkText.Split(' ');
        var overlapWords = new List<string>();

        for (var i = words.Length - 1; i >= 0; i--)
        {
            overlapWords.Insert(0, words[i]);
            var candidate = string.Join(' ', overlapWords);

            if (_tokenCounter.CountTokens(candidate) >= overlapTokens)
            {
                break;
            }
        }

        return string.Join(' ', overlapWords);
    }

    private record TextSegment(string Text, int Offset);
}
