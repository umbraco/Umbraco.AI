using Umbraco.Ai.Core.Governance;

namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// Factory for mapping between <see cref="AiExecutionSpan"/> domain models and <see cref="AiExecutionSpanEntity"/> database entities.
/// </summary>
internal static class AiExecutionSpanFactory
{
    /// <summary>
    /// Creates an <see cref="AiExecutionSpan"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiExecutionSpan BuildDomain(AiExecutionSpanEntity entity)
    {
        return new AiExecutionSpan
        {
            Id = entity.Id,
            TraceId = entity.TraceId,
            SpanId = entity.SpanId,
            ParentSpanId = entity.ParentSpanId,
            SpanName = entity.SpanName,
            SpanType = (AiExecutionSpanType)entity.SpanType,
            SequenceNumber = entity.SequenceNumber,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = (AiExecutionSpanStatus)entity.Status,
            InputData = entity.InputData,
            OutputData = entity.OutputData,
            ErrorData = entity.ErrorData,
            RetryCount = entity.RetryCount,
            TokensUsed = entity.TokensUsed
        };
    }

    /// <summary>
    /// Creates an <see cref="AiExecutionSpanEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="span">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiExecutionSpanEntity BuildEntity(AiExecutionSpan span)
    {
        return new AiExecutionSpanEntity
        {
            Id = span.Id,
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            ParentSpanId = span.ParentSpanId,
            SpanName = span.SpanName,
            SpanType = (int)span.SpanType,
            SequenceNumber = span.SequenceNumber,
            StartTime = span.StartTime,
            EndTime = span.EndTime,
            Status = (int)span.Status,
            InputData = span.InputData,
            OutputData = span.OutputData,
            ErrorData = span.ErrorData,
            RetryCount = span.RetryCount,
            TokensUsed = span.TokensUsed
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiExecutionSpanEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="span">The domain model with updated values.</param>
    public static void UpdateEntity(AiExecutionSpanEntity entity, AiExecutionSpan span)
    {
        entity.SpanId = span.SpanId;
        entity.ParentSpanId = span.ParentSpanId;
        entity.SpanName = span.SpanName;
        entity.SpanType = (int)span.SpanType;
        entity.SequenceNumber = span.SequenceNumber;
        entity.StartTime = span.StartTime;
        entity.EndTime = span.EndTime;
        entity.Status = (int)span.Status;
        entity.InputData = span.InputData;
        entity.OutputData = span.OutputData;
        entity.ErrorData = span.ErrorData;
        entity.RetryCount = span.RetryCount;
        entity.TokensUsed = span.TokensUsed;
    }
}
