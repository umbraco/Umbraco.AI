using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Chat middleware that handles audit-log tracking for AI chat operations.
/// </summary>
public sealed class AiTelemetryChatMiddleware : IAiChatMiddleware
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiTelemetryChatMiddleware"/> class.
    /// </summary>
    public AiTelemetryChatMiddleware(
        IAiAuditLogService auditLogService,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
    {
        _auditLogService = auditLogService;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new TelemetryChatClient(client, _auditLogService, _auditLogOptions);
    }

    private sealed class TelemetryChatClient : IChatClient
    {
        private readonly IChatClient _innerClient;
        private readonly IAiAuditLogService _auditLogService;
        private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

        public TelemetryChatClient(
            IChatClient innerClient,
            IAiAuditLogService auditLogService,
            IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
        {
            _innerClient = innerClient;
            _auditLogService = auditLogService;
            _auditLogOptions = auditLogOptions;
        }

        public async Task<ChatResponse> GetResponseAsync(
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
                // TODO: Handle meta data extraction

                auditLogHandle = await _auditLogService.StartAuditLogScopeAsync(
                    auditLogContext,
                    ct: cancellationToken);
            }

            try
            {
                var response = await _innerClient.GetResponseAsync(chatMessages, options, cancellationToken);

                // Complete audit-log (if exists)
                if (auditLogHandle is not null)
                {
                    await _auditLogService.CompleteAuditLogAsync(
                        auditLogHandle.AuditLog,
                        response,
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

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
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

                auditLogHandle = await _auditLogService.StartAuditLogScopeAsync(
                    auditLogContext,
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
                await _auditLogService.CompleteAuditLogAsync(
                    auditLogHandle.AuditLog,
                    null,  // No complete response for streaming
                    cancellationToken);
            }

            auditLogHandle?.Dispose();
        }

        public object? GetService(Type serviceType, object? key = null)
        {
            return _innerClient.GetService(serviceType, key);
        }

        public void Dispose()
        {
            _innerClient.Dispose();
        }
    }
}
