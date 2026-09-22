namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Shared limits for file-processing handlers that extract text from uploaded files.
/// </summary>
internal static class AIFileProcessingConstants
{
    /// <summary>
    /// The maximum number of characters a handler will return before truncating,
    /// to keep a single attached file from consuming excessive context budget.
    /// </summary>
    public const int MaxExtractedCharacters = 100_000;
}
