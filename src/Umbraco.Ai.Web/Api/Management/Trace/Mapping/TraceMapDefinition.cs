using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Web.Api.Management.Trace.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Trace.Mapping;

/// <summary>
/// Map definitions for Trace models.
/// </summary>
public class TraceMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AiTrace, TraceItemResponseModel>((_, _) => new TraceItemResponseModel(), MapToItemResponse);
        mapper.Define<AiTrace, TraceDetailResponseModel>((_, _) => new TraceDetailResponseModel(), MapToDetailResponse);
        mapper.Define<AiExecutionSpan, ExecutionSpanResponseModel>((_, _) => new ExecutionSpanResponseModel(), MapSpanToResponse);

        // Request mappings (request -> domain)
        mapper.Define<TraceFilterRequestModel, AiTraceFilter>(CreateFilterFactory, (_, _, _) => { });
    }

    private static AiTraceFilter CreateFilterFactory(TraceFilterRequestModel source, MapperContext context)
    {
        // Parse status enum if provided
        AiTraceStatus? status = null;
        if (!string.IsNullOrEmpty(source.Status))
        {
            Enum.TryParse<AiTraceStatus>(source.Status, true, out var parsedStatus);
            status = parsedStatus;
        }

        return new AiTraceFilter
        {
            Status = status,
            UserId = source.UserId,
            ProfileId = source.ProfileId,
            ProviderId = source.ProviderId,
            EntityId = source.EntityId,
            FromDate = source.FromDate,
            ToDate = source.ToDate,
            SearchText = source.SearchText
        };
    }

    // Umbraco.Code.MapAll
    private static void MapToItemResponse(AiTrace source, TraceItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.TraceId = source.TraceId;
        target.StartTime = source.StartTime;
        target.DurationMs = source.Duration.HasValue ? (long)source.Duration.Value.TotalMilliseconds : null;
        target.Status = source.Status.ToString();
        target.UserId = source.UserId;
        target.UserName = source.UserName;
        target.EntityId = source.EntityId;
        target.OperationType = source.OperationType;
        target.ModelId = source.ModelId;
        target.ProviderId = source.ProviderId;
        target.InputTokens = source.InputTokens;
        target.OutputTokens = source.OutputTokens;
        target.ErrorMessage = source.ErrorMessage;
    }

    // Umbraco.Code.MapAll -Id -TraceId -StartTime -DurationMs -Status -UserId -UserName -EntityId -OperationType -ModelId -ProviderId -InputTokens -OutputTokens -ErrorMessage
    private static void MapToDetailResponse(AiTrace source, TraceDetailResponseModel target, MapperContext context)
    {
        // First map all base properties from TraceItemResponseModel (mapped via MapToItemResponse)
        MapToItemResponse(source, target, context);

        // Then add detail-specific properties
        target.SpanId = source.SpanId;
        target.EndTime = source.EndTime;
        target.EntityType = source.EntityType;
        target.ProfileId = source.ProfileId;
        target.ProfileAlias = source.ProfileAlias;
        target.ErrorCategory = source.ErrorCategory?.ToString();
        target.TotalTokens = source.TotalTokens;
        target.PromptSnapshot = source.PromptSnapshot;
        target.ResponseSnapshot = source.ResponseSnapshot;
        target.DetailLevel = source.DetailLevel.ToString();
        target.HasSpans = source.Spans.Count > 0;
    }

    // Umbraco.Code.MapAll
    private static void MapSpanToResponse(AiExecutionSpan source, ExecutionSpanResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.TraceId = source.TraceId;
        target.SpanId = source.SpanId;
        target.ParentSpanId = source.ParentSpanId;
        target.SpanName = source.SpanName;
        target.SpanType = source.SpanType.ToString();
        target.SequenceNumber = source.SequenceNumber;
        target.StartTime = source.StartTime;
        target.EndTime = source.EndTime;
        target.DurationMs = source.Duration.HasValue ? (long)source.Duration.Value.TotalMilliseconds : null;
        target.Status = source.Status.ToString();
        target.InputData = source.InputData;
        target.OutputData = source.OutputData;
        target.ErrorData = source.ErrorData;
        target.RetryCount = source.RetryCount;
        target.TokensUsed = source.TokensUsed;
    }
}
