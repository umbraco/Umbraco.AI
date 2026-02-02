using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Factory implementation for creating AIAuditLog instances.
/// </summary>
internal sealed class AIAuditLogFactory : IAiAuditLogFactory
{
    private readonly IOptionsMonitor<AIAuditLogOptions> _options;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly ILogger<AIAuditLogFactory> _logger;

    public AIAuditLogFactory(
        IOptionsMonitor<AIAuditLogOptions> options,
        IBackOfficeSecurityAccessor securityAccessor,
        ILogger<AIAuditLogFactory> logger)
    {
        _options = options;
        _securityAccessor = securityAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public AIAuditLog Create(
        AIAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? parentId = null)
    {
        if (!context.ProfileId.HasValue)
            throw new ArgumentException("ProfileId must be set in the AIAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ProfileAlias))
            throw new ArgumentException("ProfileAlias must be set in the AIAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ProviderId))
            throw new ArgumentException("ProviderId must be set in the AIAuditContext.", nameof(context));
        if (string.IsNullOrWhiteSpace(context.ModelId))
            throw new ArgumentException("ModelId must be set in the AIAuditContext.", nameof(context));

        var user = _securityAccessor.BackOfficeSecurity?.CurrentUser;
        var detailLevel = _options.CurrentValue.DetailLevel;

        var auditLog = new AIAuditLog
        {
            Id = Guid.NewGuid(),
            ParentAuditLogId = parentId,
            UserId = user?.Key.ToString(),
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
            Status = AIAuditLogStatus.Running,
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

    private const int MaxArgumentLength = 500;
    private const int MaxResultLength = 1000;

    private static string? FormatPromptSnapshot(object? promptObj, AICapability capability)
    {
        if (promptObj is null)
        {
            return null;
        }

        try
        {
            return capability switch
            {
                AICapability.Chat when promptObj is IEnumerable<ChatMessage> messages =>
                    string.Join("\n", messages.Select(FormatChatMessage)),

                AICapability.Embedding when promptObj is IEnumerable<string> values =>
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

    private static string FormatChatMessage(ChatMessage message)
    {
        var parts = new List<string>();

        foreach (var content in message.Contents)
        {
            var formatted = FormatContent(content, message.Role);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }

        // If no content was formatted, just return the role
        if (parts.Count == 0)
        {
            return $"[{message.Role}]";
        }

        return string.Join("\n", parts);
    }

    private static string? FormatContent(AIContent content, ChatRole role)
    {
        return content switch
        {
            TextContent textContent =>
                $"[{role}] {textContent.Text}",

            FunctionCallContent functionCall =>
                FormatFunctionCall(functionCall),

            FunctionResultContent functionResult =>
                FormatFunctionResult(functionResult),

            DataContent dataContent =>
                FormatDataContent(dataContent),

            _ => null
        };
    }

    private static string FormatFunctionCall(FunctionCallContent functionCall)
    {
        var args = FormatArguments(functionCall.Arguments);
        if (args.Length > MaxArgumentLength)
        {
            args = $"{args[..MaxArgumentLength]}... (truncated, {args.Length} chars)";
        }
        return $"[tool_call:{functionCall.CallId}] {functionCall.Name}({args})";
    }

    private static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "{}";
        }

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(arguments);
        }
        catch
        {
            return "{}";
        }
    }

    private static string FormatFunctionResult(FunctionResultContent functionResult)
    {
        var result = FormatResult(functionResult.Result);
        if (result.Length > MaxResultLength)
        {
            result = $"{result[..MaxResultLength]}... (truncated, {result.Length} chars)";
        }
        return $"[tool:{functionResult.CallId}] -> {result}";
    }

    private static string FormatResult(object? result)
    {
        if (result is null)
        {
            return "(null)";
        }

        if (result is string strResult)
        {
            return strResult;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch
        {
            return result.ToString() ?? "(null)";
        }
    }

    private static string FormatDataContent(DataContent dataContent)
    {
        var size = dataContent.Data.Length;
        return $"[data:{dataContent.MediaType}] ({size} bytes)";
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
