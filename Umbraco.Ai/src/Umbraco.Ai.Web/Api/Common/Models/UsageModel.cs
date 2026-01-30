namespace Umbraco.Ai.Web.Api.Common.Models;

/// <summary>
/// Usage statistics for an AI request.
/// </summary>
public class UsageModel
{
    /// <summary>
    /// The number of tokens in the input.
    /// </summary>
    public long? InputTokens { get; set; }

    /// <summary>
    /// The number of tokens in the output.
    /// </summary>
    public long? OutputTokens { get; set; }

    /// <summary>
    /// The total number of tokens used.
    /// </summary>
    public long? TotalTokens { get; set; }
}
