using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Management.Audit.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Audit.Mapping;

/// <summary>
/// Map definitions for Audit models.
/// </summary>
public class AuditMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AiAudit, AuditItemResponseModel>((_, _) => new AuditItemResponseModel(), MapToItemResponse);
        mapper.Define<AiAudit, AuditDetailResponseModel>((_, _) => new AuditDetailResponseModel(), MapToDetailResponse);
        mapper.Define<AiAuditActivity, AuditActivityResponseModel>((_, _) => new AuditActivityResponseModel(), MapActivityToResponse);

        // Request mappings (request -> domain)
        mapper.Define<AuditFilterRequestModel, AiAuditFilter>(CreateFilterFactory, (_, _, _) => { });
    }

    private static AiAuditFilter CreateFilterFactory(AuditFilterRequestModel source, MapperContext context)
    {
        // Parse status enum if provided
        AiAuditStatus? status = null;
        if (!string.IsNullOrEmpty(source.Status))
        {
            Enum.TryParse<AiAuditStatus>(source.Status, true, out var parsedStatus);
            status = parsedStatus;
        }

        return new AiAuditFilter
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
    private static void MapToItemResponse(AiAudit source, AuditItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.StartTime = source.StartTime;
        target.DurationMs = source.Duration.HasValue ? (long)source.Duration.Value.TotalMilliseconds : null;
        target.Status = source.Status.ToString();
        target.UserId = source.UserId;
        target.UserName = source.UserName;
        target.EntityId = source.EntityId;
        target.OperationType = source.OperationType;
        target.ModelId = source.ModelId;
        target.ProviderId = source.ProviderId;
        target.FeatureType = source.FeatureType;
        target.FeatureId = source.FeatureId;
        target.InputTokens = source.InputTokens;
        target.OutputTokens = source.OutputTokens;
        target.ErrorMessage = source.ErrorMessage;
    }

    // Umbraco.Code.MapAll -Id -AuditId -StartTime -DurationMs -Status -UserId -UserName -EntityId -OperationType -ModelId -ProviderId -FeatureType -FeatureId -InputTokens -OutputTokens -ErrorMessage
    private static void MapToDetailResponse(AiAudit source, AuditDetailResponseModel target, MapperContext context)
    {
        // First map all base properties from AuditItemResponseModel (mapped via MapToItemResponse)
        MapToItemResponse(source, target, context);

        // Then add detail-specific properties
        target.EndTime = source.EndTime;
        target.EntityType = source.EntityType;
        target.ProfileId = source.ProfileId;
        target.ProfileAlias = source.ProfileAlias;
        target.ErrorCategory = source.ErrorCategory?.ToString();
        target.TotalTokens = source.TotalTokens;
        target.PromptSnapshot = source.PromptSnapshot;
        target.ResponseSnapshot = source.ResponseSnapshot;
        target.DetailLevel = source.DetailLevel.ToString();
        target.HasSpans = source.Activities.Count > 0;
    }

    // Umbraco.Code.MapAll
    private static void MapActivityToResponse(AiAuditActivity source, AuditActivityResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.AuditId = source.AuditId;
        target.ActivityName = source.ActivityName;
        target.ActivityType = source.ActivityType.ToString();
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
