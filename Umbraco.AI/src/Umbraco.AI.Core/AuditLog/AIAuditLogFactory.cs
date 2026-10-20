using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Factory implementation for creating AIAuditLog instances.
/// </summary>
internal sealed class AIAuditLogFactory : IAIAuditLogFactory
{
    private readonly IBackOfficeSecurityAccessor _securityAccessor;

    public AIAuditLogFactory(IBackOfficeSecurityAccessor securityAccessor)
        => _securityAccessor = securityAccessor;

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
            FeatureAlias = context.FeatureAlias,
            ProfileVersion = context.ProfileVersion,
            FeatureVersion = context.FeatureVersion,
            Metadata = metadata != null
                ? new Dictionary<string, string>(metadata)
                : null,

            // Set initial run state
            Status = AIAuditLogStatus.Running,
            StartTime = DateTime.UtcNow,
        };

        return auditLog;
    }

    // Relaxed escaping keeps characters such as + ' < > and non-ASCII letters as they are rather than
    // \uXXXX escapes, so redaction patterns match the text the tool actually sent. Snapshots are shown
    // as plain text, never as HTML.
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Formats a prompt for storage in the audit log.
    /// </summary>
    /// <param name="promptObj">The prompt data.</param>
    /// <param name="capability">The capability the prompt was sent to.</param>
    /// <param name="redact">
    /// Applied to tool arguments and results before they are truncated, so a value cut at the limit can't
    /// slip past a redaction pattern. The caller still redacts the whole snapshot afterwards.
    /// </param>
    /// <param name="options">Supplies the tool argument and result length limits; defaults apply when null.</param>
    internal static string? FormatPromptSnapshot(
        object? promptObj,
        AICapability capability,
        Func<string, string>? redact = null,
        AIAuditLogOptions? options = null)
    {
        options ??= new AIAuditLogOptions();

        if (promptObj is null)
        {
            return null;
        }

        try
        {
            return capability switch
            {
                AICapability.Chat when promptObj is IEnumerable<ChatMessage> messages =>
                    string.Join("\n\n", messages.Select(m => FormatChatMessage(m, redact, options))),

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

    private static string FormatChatMessage(ChatMessage message, Func<string, string>? redact, AIAuditLogOptions options)
    {
        var parts = new List<string>();

        foreach (var content in message.Contents)
        {
            var formatted = FormatContent(content, redact, options);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }

        var header = $"===== [{message.Role}] =====";

        // If no content was formatted, just return the header
        if (parts.Count == 0)
        {
            return header;
        }

        return $"{header}\n{string.Join("\n", parts)}";
    }

    private static string? FormatContent(AIContent content, Func<string, string>? redact, AIAuditLogOptions options)
    {
        return content switch
        {
            TextContent textContent =>
                textContent.Text,

            FunctionCallContent functionCall =>
                FormatFunctionCall(functionCall, redact, options.MaxToolArgumentsLength),

            FunctionResultContent functionResult =>
                FormatFunctionResult(functionResult, redact, options.MaxToolResultLength),

            DataContent dataContent =>
                FormatDataContent(dataContent),

            _ => null
        };
    }

    private static string FormatFunctionCall(FunctionCallContent functionCall, Func<string, string>? redact, int maxLength)
    {
        var args = TruncateIfNeeded(Redact(FormatArguments(functionCall.Arguments), redact), maxLength);
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
            return JsonSerializer.Serialize(arguments, s_jsonOptions);
        }
        catch
        {
            return "{}";
        }
    }

    private static string FormatFunctionResult(FunctionResultContent functionResult, Func<string, string>? redact, int maxLength)
    {
        var result = TruncateIfNeeded(Redact(FormatResult(functionResult.Result), redact), maxLength);
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
            return JsonSerializer.Serialize(result, s_jsonOptions);
        }
        catch
        {
            return result.ToString() ?? "(null)";
        }
    }

    private static string Redact(string value, Func<string, string>? redact)
        => redact is null ? value : redact(value);

    private static string TruncateIfNeeded(string value, int maxLength)
        => maxLength <= 0 || value.Length <= maxLength
            ? value
            : $"{value[..maxLength]}... (truncated, {value.Length} chars)";

    private static string FormatDataContent(DataContent dataContent)
    {
        var size = dataContent.Data.Length;
        return $"[data:{dataContent.MediaType}] ({size} bytes)";
    }
}
