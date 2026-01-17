using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Management.AuditLog.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.AuditLog.Mapping;

/// <summary>
/// Map definitions for AuditLog models.
/// </summary>
public class AuditLogMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AiAuditLog, AuditLogItemResponseModel>((_, _) => new AuditLogItemResponseModel(), MapToItemResponse);
        mapper.Define<AiAuditLog, AuditLogDetailResponseModel>((_, _) => new AuditLogDetailResponseModel(), MapToDetailResponse);

        // Request mappings (request -> domain)
        mapper.Define<AuditLogFilterRequestModel, AiAuditLogFilter>(CreateFilterFactory, (_, _, _) => { });
    }

    private static AiAuditLogFilter CreateFilterFactory(AuditLogFilterRequestModel source, MapperContext context)
    {
        // Parse status enum if provided
        AiAuditLogStatus? status = null;
        if (!string.IsNullOrEmpty(source.Status))
        {
            Enum.TryParse<AiAuditLogStatus>(source.Status, true, out var parsedStatus);
            status = parsedStatus;
        }

        // Parse capability enum if provided
        AiCapability? capability = null;
        if (!string.IsNullOrEmpty(source.Capability))
        {
            Enum.TryParse<AiCapability>(source.Capability, true, out var parsedCapability);
            capability = parsedCapability;
        }

        return new AiAuditLogFilter
        {
            Status = status,
            UserId = source.UserId,
            ProfileId = source.ProfileId,
            ProviderId = source.ProviderId,
            Capability = capability,
            FeatureType = source.FeatureType,
            FeatureId = source.FeatureId,
            EntityId = source.EntityId,
            EntityType = source.EntityType,
            ParentAuditLogId = source.ParentAuditLogId,
            FromDate = source.FromDate,
            ToDate = source.ToDate,
            SearchText = source.SearchText
        };
    }

    // Umbraco.Code.MapAll
    private static void MapToItemResponse(AiAuditLog source, AuditLogItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.StartTime = source.StartTime;
        target.DurationMs = source.Duration.HasValue ? (long)source.Duration.Value.TotalMilliseconds : null;
        target.Status = source.Status.ToString();
        target.UserId = source.UserId;
        target.UserName = source.UserName;
        target.EntityId = source.EntityId;
        target.Capability = source.Capability.ToString();
        target.ProfileId = source.ProfileId.ToString();
        target.ProfileAlias = source.ProfileAlias;
        target.ProviderId = source.ProviderId;
        target.ModelId = source.ModelId;
        target.FeatureType = source.FeatureType;
        target.FeatureId = source.FeatureId;
        target.ParentAuditLogId = source.ParentAuditLogId;
        target.InputTokens = source.InputTokens;
        target.OutputTokens = source.OutputTokens;
        target.ErrorMessage = source.ErrorMessage;
    }

    // Umbraco.Code.MapAll -Id -AuditLogId -StartTime -DurationMs -Status -UserId -UserName -EntityId -Capability -ModelId -ProviderId -FeatureType -FeatureId -ParentAuditLogId -Metadata -InputTokens -OutputTokens -ErrorMessage
    private static void MapToDetailResponse(AiAuditLog source, AuditLogDetailResponseModel target, MapperContext context)
    {
        // First map all base properties from AuditLogItemResponseModel (mapped via MapToItemResponse)
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
        target.Metadata = source.Metadata is not null ? new Dictionary<string, string>(source.Metadata) : null;
    }
}
