using System.Text.Json;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Persistence.AuditLog;

/// <summary>
/// Factory for mapping between <see cref="AiAuditLog"/> domain models and <see cref="AiAuditLogEntity"/> database entities.
/// </summary>
internal static class AiAuditLogFactory
{
    /// <summary>
    /// Creates an <see cref="AiAuditLog"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiAuditLog BuildDomain(AiAuditLogEntity entity)
    {
        // Deserialize Metadata if present
        IReadOnlyDictionary<string, string>? metadata = null;
        if (!string.IsNullOrWhiteSpace(entity.Metadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata);
            }
            catch (JsonException)
            {
                // Log warning but don't fail - metadata is optional
            }
        }

        return new AiAuditLog
        {
            Id = entity.Id,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = (AiAuditLogStatus)entity.Status,
            ErrorCategory = entity.ErrorCategory.HasValue ? (AiAuditLogErrorCategory)entity.ErrorCategory.Value : null,
            ErrorMessage = entity.ErrorMessage,
            UserId = entity.UserId,
            UserName = entity.UserName,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            Capability = (AiCapability)entity.Capability, // Changed from OperationType string to Capability enum
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            ProfileVersion = entity.ProfileVersion,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            FeatureType = entity.FeatureType,
            FeatureId = entity.FeatureId,
            FeatureVersion = entity.FeatureVersion,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            PromptSnapshot = entity.PromptSnapshot,
            ResponseSnapshot = entity.ResponseSnapshot,
            DetailLevel = (AiAuditLogDetailLevel)entity.DetailLevel,
            ParentAuditLogId = entity.ParentAuditLogId, // New: Parent audit-log tracking
            Metadata = metadata // New: Extensible metadata
        };
    }

    /// <summary>
    /// Creates an <see cref="AiAuditLogEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="audit">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiAuditLogEntity BuildEntity(AiAuditLog audit)
    {
        // Serialize Metadata if present
        string? metadataJson = null;
        if (audit.Metadata != null && audit.Metadata.Count > 0)
        {
            metadataJson = JsonSerializer.Serialize(audit.Metadata);
        }

        return new AiAuditLogEntity
        {
            Id = audit.Id,
            StartTime = audit.StartTime,
            EndTime = audit.EndTime,
            Status = (int)audit.Status,
            ErrorCategory = audit.ErrorCategory.HasValue ? (int)audit.ErrorCategory.Value : null,
            ErrorMessage = audit.ErrorMessage,
            UserId = audit.UserId,
            UserName = audit.UserName,
            EntityId = audit.EntityId,
            EntityType = audit.EntityType,
            Capability = (int)audit.Capability, // Changed from OperationType string to Capability enum
            ProfileId = audit.ProfileId,
            ProfileAlias = audit.ProfileAlias,
            ProfileVersion = audit.ProfileVersion,
            ProviderId = audit.ProviderId,
            ModelId = audit.ModelId,
            FeatureType = audit.FeatureType,
            FeatureId = audit.FeatureId,
            FeatureVersion = audit.FeatureVersion,
            InputTokens = audit.InputTokens,
            OutputTokens = audit.OutputTokens,
            TotalTokens = audit.TotalTokens,
            PromptSnapshot = audit.PromptSnapshot,
            ResponseSnapshot = audit.ResponseSnapshot,
            DetailLevel = (int)audit.DetailLevel,
            ParentAuditLogId = audit.ParentAuditLogId, // New: Parent audit-log tracking
            Metadata = metadataJson // New: Extensible metadata
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiAuditLogEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="audit">The domain model with updated values.</param>
    public static void UpdateEntity(AiAuditLogEntity entity, AiAuditLog audit)
    {
        // Serialize Metadata if present
        string? metadataJson = null;
        if (audit.Metadata != null && audit.Metadata.Count > 0)
        {
            metadataJson = JsonSerializer.Serialize(audit.Metadata);
        }

        entity.StartTime = audit.StartTime;
        entity.EndTime = audit.EndTime;
        entity.Status = (int)audit.Status;
        entity.ErrorCategory = audit.ErrorCategory.HasValue ? (int)audit.ErrorCategory.Value : null;
        entity.ErrorMessage = audit.ErrorMessage;
        entity.UserId = audit.UserId;
        entity.UserName = audit.UserName;
        entity.EntityId = audit.EntityId;
        entity.EntityType = audit.EntityType;
        entity.Capability = (int)audit.Capability; // Changed from OperationType string to Capability enum
        entity.ProfileId = audit.ProfileId;
        entity.ProfileAlias = audit.ProfileAlias;
        entity.ProfileVersion = audit.ProfileVersion;
        entity.ProviderId = audit.ProviderId;
        entity.ModelId = audit.ModelId;
        entity.FeatureType = audit.FeatureType;
        entity.FeatureId = audit.FeatureId;
        entity.FeatureVersion = audit.FeatureVersion;
        entity.InputTokens = audit.InputTokens;
        entity.OutputTokens = audit.OutputTokens;
        entity.TotalTokens = audit.TotalTokens;
        entity.PromptSnapshot = audit.PromptSnapshot;
        entity.ResponseSnapshot = audit.ResponseSnapshot;
        entity.DetailLevel = (int)audit.DetailLevel;
        entity.ParentAuditLogId = audit.ParentAuditLogId; // New: Parent audit-log tracking
        entity.Metadata = metadataJson; // New: Extensible metadata
    }
}
