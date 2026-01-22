namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Factory for creating AiUsageRecord instances with proper dependency injection.
/// </summary>
internal interface IAiUsageRecordFactory
{
    /// <summary>
    /// Creates a new AiUsageRecord instance from the given context and result.
    /// </summary>
    /// <param name="context">The usage context containing operation metadata.</param>
    /// <param name="result">The operation result containing metrics and status.</param>
    /// <returns>A new AiUsageRecord instance ready to be persisted.</returns>
    AiUsageRecord Create(
        AiUsageRecordContext context,
        AiUsageRecordResult result);
}
