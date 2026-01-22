using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Factory implementation for creating AiAuditLog instances.
/// </summary>
internal sealed class AiAuditLogFactory : IAiAuditLogFactory
{
    private readonly IOptionsMonitor<AiAuditLogOptions> _options;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly ILogger<AiAuditLogFactory> _logger;

    public AiAuditLogFactory(
        IOptionsMonitor<AiAuditLogOptions> options,
        IBackOfficeSecurityAccessor securityAccessor,
        ILogger<AiAuditLogFactory> logger)
    {
        _options = options;
        _securityAccessor = securityAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiAuditLog Create(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? parentId = null)
    {
        if (!context.ProfileId.HasValue)
            throw new ArgumentException("ProfileId must be set in the AiAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ProfileAlias))
            throw new ArgumentException("ProfileAlias must be set in the AiAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ProviderId))
            throw new ArgumentException("ProviderId must be set in the AiAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ModelId))
            throw new ArgumentException("ModelId must be set in the AiAuditContext.", nameof(context));

        var user = _securityAccessor.BackOfficeSecurity?.CurrentUser;
        var detailLevel = _options.CurrentValue.DetailLevel;

        var auditLog = new AiAuditLog
        {
            Id = Guid.NewGuid(),
            ParentAuditLogId = parentId,
            UserId = user?.Id.ToString(),
            UserName = user?.Name,
            Capability = context.Capability,
            ProfileId = context.ProfileId.Value,
            ProfileAlias = context.ProfileAlias,
            ProviderId = context.ProviderId,
            ModelId = context.ModelId,
            EntityId = context.EntityId,
            EntityType = context.EntityType,
            FeatureType = context.FeatureType,
            FeatureId = context.FeatureId,
            ProfileVersion = context.ProfileVersion,
            FeatureVersion = context.FeatureVersion,
            Metadata = metadata != null
                ? new Dictionary<string, string>(metadata)
                : null,
            DetailLevel = detailLevel,
            
            // Set initial run state
            Status = AiAuditLogStatus.Running,
            StartTime = DateTime.UtcNow,
        };

        // Capture prompt snapshot if configured
        if (_options.CurrentValue.PersistPrompts && context.Prompt is not null)
        {
            var prompt = FormatPromptSnapshot(context.Prompt, context.Capability);
            prompt = ApplyRedaction(prompt);
            auditLog.PromptSnapshot = prompt;
            
            _logger.LogDebug("Captured prompt snapshot for audit-log {AuditLogId}: {Length} characters",
                auditLog.Id, prompt?.Length ?? 0);
        }

        return auditLog;
    }

    private static string? FormatPromptSnapshot(object? promptObj, AiCapability capability)
    {
        if (promptObj is null)
        {
            return null;
        }

        try
        {
            return capability switch
            {
                AiCapability.Chat when promptObj is IEnumerable<ChatMessage> messages =>
                    string.Join("\n", messages.Select(m => $"[{m.Role}] {m.Text}")),

                AiCapability.Embedding when promptObj is IEnumerable<string> values =>
                    string.Join("\n", values.Select((v, i) => $"[{i}] {v}")),

                _ => promptObj.ToString()
            };
        }
        catch
        {
            // If formatting fails, return a fallback representation
            return $"[Unable to format {capability} prompt]";
        }
    }

    private string? ApplyRedaction(string? input)
    {
        if (string.IsNullOrEmpty(input) || _options.CurrentValue.RedactionPatterns.Count == 0)
            return input;

        var result = input;
        foreach (var pattern in _options.CurrentValue.RedactionPatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply redaction pattern: {Pattern}", pattern);
            }
        }
        return result;
    }
}
