using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

internal sealed class AiAuditingChatClient : DelegatingChatClient
{
    private readonly IChatClient _innerClient;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    public AiAuditingChatClient(
        IChatClient innerClient,
        IAiAuditLogService auditLogService,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
        : base(innerClient)
    {
        _innerClient = innerClient;
        _auditLogService = auditLogService;
        _auditLogOptions = auditLogOptions;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Start audit-log recording if enabled
        AiAuditScopeHandle? auditLogHandle = null;
        
        if (_auditLogOptions.CurrentValue.Enabled)
        {
            var auditLogContext = AiAuditContext.ExtractFromOptions(
                AiCapability.Chat,
                options,
                chatMessages.ToList());

            // Extract metadata from options if present
            Dictionary<string, string>? metadata = null;
            if (options?.AdditionalProperties?.TryGetValue(Constants.MetadataKeys.LogKeys, out var logKeys) == true
                && logKeys is IEnumerable<string> keys)
            {
                metadata = keys.ToDictionary(
                    key => key,
                    key => options?.AdditionalProperties?[key]?.ToString() ?? string.Empty);
            }

            auditLogHandle = await _auditLogService.StartAuditLogScopeAsync(
                auditLogContext,
                metadata: metadata,
                ct: cancellationToken);
        }

        try
        {
            var response = await _innerClient.GetResponseAsync(chatMessages, options, cancellationToken);

            // Complete audit-log (if exists)
            if (auditLogHandle is not null)
            {
                var trackingChatClient = _innerClient.GetService<AiTrackingChatClient>();

                await _auditLogService.CompleteAuditLogAsync(
                    auditLogHandle.AuditLog,
                    new AiAuditResponse
                    {
                        Data = trackingChatClient?.LastResponse,
                        Usage = trackingChatClient?.LastUsageDetails,
                    },
                    cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            // Record audit-log failure (if exists)
            if (auditLogHandle is not null)
            {
                await _auditLogService.RecordAuditLogFailureAsync(
                    auditLogHandle.AuditLog,
                    ex,
                    cancellationToken);
            }

            throw;
        }
        finally
        {
            auditLogHandle?.Dispose();
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Start audit-log recording if enabled
        AiAuditScopeHandle? auditLogHandle = null;
        if (_auditLogOptions.CurrentValue.Enabled)
        {
            var auditLogContext = AiAuditContext.ExtractFromOptions(
                AiCapability.Chat,
                options,
                chatMessages.ToList());

            Dictionary<string, string>? metadata = null;
            if (options?.AdditionalProperties?.TryGetValue(Constants.MetadataKeys.LogKeys, out var logKeys) == true
                && logKeys is IEnumerable<string> keys)
            {
                metadata = keys.ToDictionary(
                    key => key,
                    key => options?.AdditionalProperties?[key]?.ToString() ?? string.Empty);
            }
            
            auditLogHandle = await _auditLogService.StartAuditLogScopeAsync(
                auditLogContext,
                metadata: metadata,
                ct: cancellationToken);
        }

        IAsyncEnumerable<ChatResponseUpdate> stream;

        try
        {
            stream = _innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            // Record audit-log failure (if exists)
            if (auditLogHandle is not null)
            {
                await _auditLogService.RecordAuditLogFailureAsync(
                    auditLogHandle.AuditLog,
                    ex,
                    cancellationToken);
            }

            auditLogHandle?.Dispose();
            throw;
        }
        
        // Stream updates - audit-log completion handled after streaming completes
        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }

        // Mark audit-log as completed (no response capture for streaming)
        if (auditLogHandle is not null)
        {
            var trackingChatClient = _innerClient.GetService<AiTrackingChatClient>();

            await _auditLogService.CompleteAuditLogAsync(
                auditLogHandle.AuditLog,
                new AiAuditResponse
                {
                    Data = trackingChatClient?.LastResponse,
                    Usage = trackingChatClient?.LastUsageDetails,
                },
                cancellationToken);
        }

        auditLogHandle?.Dispose();
    }
}