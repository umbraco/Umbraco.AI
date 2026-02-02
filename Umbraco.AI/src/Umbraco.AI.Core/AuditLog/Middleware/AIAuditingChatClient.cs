using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog.Middleware;

internal sealed class AIAuditingChatClient : DelegatingChatClient
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditingChatClient(
        IChatClient innerClient,
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiAuditLogService auditLogService,
        IAiAuditLogFactory auditLogFactory,
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
        // Start audit-log recording if enabled
        AIAuditScope? auditScope = null;
        AIAuditLog? auditLog = null;

        if (_auditLogOptions.CurrentValue.Enabled && _runtimeContextAccessor.Context is not null)
        {
            // Extract audit context from options and messages
            var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
                AICapability.Chat,
                _runtimeContextAccessor.Context,
                chatMessages.ToList());

            // Extract metadata from RuntimeContext if present
            Dictionary<string, string>? metadata = null;
            var context = _runtimeContextAccessor.Context;
            if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) == true)
            {
                metadata = logKeys.ToDictionary(
                    key => key,
                    key => context.GetValue<object?>(key)?.ToString() ?? string.Empty);
            }

            // Create audit-log entry using factory
            auditLog = _auditLogFactory.Create(
                auditLogContext,
                metadata,
                parentId: AIAuditScope.Current?.AuditLogId); // Capture parent from ambient scope

            // Create scope synchronously (for nested operation tracking)
            auditScope = AIAuditScope.Begin(auditLog.Id);

            // Queue persistence in background (fire-and-forget)
            await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);
        }

        try
        {
            var response = await InnerClient.GetResponseAsync(chatMessages, options, cancellationToken);

            // Complete audit-log (if exists)
            if (auditLog is not null)
            {
                var trackingChatClient = InnerClient.GetService<AITrackingChatClient>();

                // Queue completion in background (fire-and-forget)
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    new AIAuditResponse
                    {
                        Data = trackingChatClient?.LastResponseMessages,
                        Usage = trackingChatClient?.LastUsageDetails,
                    },
                    cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            // Record audit-log failure (if exists)
            if (auditLog is not null)
            {
                // Queue failure in background (fire-and-forget)
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog,
                    ex,
                    cancellationToken);
            }

            throw;
        }
        finally
        {
            // Dispose scope to restore previous audit context
            auditScope?.Dispose();
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Start audit-log recording if enabled
        AIAuditScope? auditScope = null;
        AIAuditLog? auditLog = null;

        if (_auditLogOptions.CurrentValue.Enabled && _runtimeContextAccessor.Context is not null)
        {
            // Extract audit context from options and messages
            var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
                AICapability.Chat,
                _runtimeContextAccessor.Context,
                chatMessages.ToList());

            // Extract metadata from RuntimeContext if present
            Dictionary<string, string>? metadata = null;
            var context = _runtimeContextAccessor.Context;
            if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) == true)
            {
                metadata = logKeys.ToDictionary(
                    key => key,
                    key => context.GetValue<object?>(key)?.ToString() ?? string.Empty);
            }

            // Create audit-log entry using factory
            auditLog = _auditLogFactory.Create(
                auditLogContext,
                metadata,
                parentId: AIAuditScope.Current?.AuditLogId); // Capture parent from ambient scope

            // Create scope synchronously (for nested operation tracking)
            auditScope = AIAuditScope.Begin(auditLog.Id);

            // Queue persistence in background (fire-and-forget)
            await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);
        }

        IAsyncEnumerable<ChatResponseUpdate> stream;

        try
        {
            stream = InnerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            // Record audit-log failure (if exists)
            if (auditLog is not null)
            {
                // Queue failure in background (fire-and-forget)
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog,
                    ex,
                    cancellationToken);
            }

            auditScope?.Dispose();
            throw;
        }

        // Stream updates - audit-log completion handled after streaming completes
        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }

        // Mark audit-log as completed (no response capture for streaming)
        if (auditLog is not null)
        {
            var trackingChatClient = InnerClient.GetService<AITrackingChatClient>();

            // Queue completion in background (fire-and-forget)
            await _auditLogService.QueueCompleteAuditLogAsync(
                auditLog,
                new AIAuditResponse
                {
                    Data = trackingChatClient?.LastResponseMessages,
                    Usage = trackingChatClient?.LastUsageDetails,
                },
                cancellationToken);
        }

        // Dispose scope to restore previous audit context
        auditScope?.Dispose();
    }
}