using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog.Middleware;

internal sealed class AIAuditingChatClient : DelegatingChatClient
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditingChatClient(
        IChatClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var (auditScope, auditLog) = await StartAuditLogAsync(
            messages, cancellationToken);

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _runtimeContextAccessor);

        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = messages, Capability = AICapability.Chat }
            : null;

        try
        {
            var response = await InnerClient.GetResponseAsync(chatMessages, options, cancellationToken);

            // Complete audit-log (if exists)
            // Use CancellationToken.None so the status update is always persisted,
            // even if the original request was cancelled (e.g. client disconnected)
            if (auditLog is not null)
            {
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    auditPrompt,
                    new AIAuditResponse
                    {
                        Data = response.Messages,
                        Usage = response.Usage,
                    },
                    CancellationToken.None);
            }

            return response;
        }
        catch (Exception ex)
        {
            if (auditLog is not null)
            {
                // Use CancellationToken.None so the failure status is always persisted,
                // even if the original request was cancelled (e.g. client disconnected)
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, CancellationToken.None);
            }

            throw;
        }
        finally
        {
            auditScope?.Dispose();
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var (auditScope, auditLog) = await StartAuditLogAsync(
            messages, cancellationToken);

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _runtimeContextAccessor);

        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = messages, Capability = AICapability.Chat }
            : null;

        IAsyncEnumerable<ChatResponseUpdate> stream;

        try
        {
            stream = InnerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            if (auditLog is not null)
            {
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, cancellationToken);
            }

            auditScope?.Dispose();
            throw;
        }

        // Stream updates - wrap in error-capturing enumerator to handle
        // exceptions thrown during iteration (e.g., guardrail blocks)
        await foreach (var update in WrapStreamWithErrorCapture(
            stream, auditLog, auditPrompt, auditScope, cancellationToken))
        {
            yield return update;
        }

        // Mark audit-log as completed (only reached if no exception during streaming)
        // Use CancellationToken.None so the status update is always persisted,
        // even if the original request was cancelled (e.g. client disconnected)
        if (auditLog is not null)
        {
            var trackingChatClient = InnerClient.GetService<AITrackingChatClient>();

            await _auditLogService.QueueCompleteAuditLogAsync(
                auditLog,
                auditPrompt,
                new AIAuditResponse
                {
                    Data = trackingChatClient?.LastResponseMessages,
                    Usage = trackingChatClient?.LastUsageDetails,
                },
                CancellationToken.None);
        }

        auditScope?.Dispose();
    }

    /// <summary>
    /// Wraps an async stream to capture exceptions during iteration
    /// and record them as audit log failures. This is needed because C# does not
    /// allow yield return inside try-catch blocks.
    /// </summary>
    private async IAsyncEnumerable<ChatResponseUpdate> WrapStreamWithErrorCapture(
        IAsyncEnumerable<ChatResponseUpdate> stream,
        AIAuditLog? auditLog,
        AIAuditPrompt? auditPrompt,
        AIAuditScope? auditScope,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate current;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                current = enumerator.Current;
            }
            catch (Exception ex)
            {
                if (auditLog is not null)
                {
                    // Use CancellationToken.None so the failure status is always persisted,
                    // even if the original request was cancelled (e.g. client disconnected)
                    await _auditLogService.QueueRecordAuditLogFailureAsync(
                        auditLog, auditPrompt, ex, CancellationToken.None);
                }

                auditScope?.Dispose();
                throw;
            }

            yield return current;
        }
    }

    /// <summary>
    /// Creates audit log and scope if audit logging is enabled and runtime context is available.
    /// </summary>
    private async Task<(AIAuditScope? Scope, AIAuditLog? AuditLog)> StartAuditLogAsync(
        List<ChatMessage> chatMessages,
        CancellationToken cancellationToken)
    {
        if (!_auditLogOptions.CurrentValue.Enabled || _runtimeContextAccessor.Context is null)
        {
            return (null, null);
        }

        var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
            AICapability.Chat,
            _runtimeContextAccessor.Context,
            chatMessages);

        // Extract metadata from RuntimeContext if present
        Dictionary<string, string>? metadata = null;
        var context = _runtimeContextAccessor.Context;
        if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) == true)
        {
            metadata = logKeys.ToDictionary(
                key => key,
                key => context.GetValue<object?>(key)?.ToString() ?? string.Empty);
        }

        var auditLog = _auditLogFactory.Create(
            auditLogContext,
            metadata,
            parentId: AIAuditScope.Current?.AuditLogId);

        var auditScope = AIAuditScope.Begin(auditLog.Id);

        // Capture TraceId from ambient Activity (created by OpenTelemetry middleware)
        auditLog.TraceId = Activity.Current?.TraceId.ToString();

        await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);

        return (auditScope, auditLog);
    }
}
