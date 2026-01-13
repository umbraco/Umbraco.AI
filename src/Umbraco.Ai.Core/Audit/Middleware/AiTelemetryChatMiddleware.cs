using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Audit.Middleware;

/// <summary>
/// Chat middleware that handles both OpenTelemetry Activities (external observability)
/// and IAiAuditService calls (internal governance/audit tracking) as independent concerns.
/// </summary>
public sealed class AiTelemetryChatMiddleware : IAiChatMiddleware
{
    private readonly IAiAuditService? _auditService;
    private readonly IBackOfficeSecurityAccessor? _securityAccessor;
    private readonly IOptionsMonitor<AiAuditOptions> _auditOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiTelemetryChatMiddleware"/> class.
    /// </summary>
    public AiTelemetryChatMiddleware(
        IAiAuditService? auditService,
        IBackOfficeSecurityAccessor? securityAccessor,
        IOptionsMonitor<AiAuditOptions> auditOptions)
    {
        _auditService = auditService;
        _securityAccessor = securityAccessor;
        _auditOptions = auditOptions;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new TelemetryChatClient(client, _auditService, _securityAccessor, _auditOptions);
    }

    private sealed class TelemetryChatClient : IChatClient
    {
        private readonly IChatClient _innerClient;
        private readonly IAiAuditService? _auditService;
        private readonly IBackOfficeSecurityAccessor? _securityAccessor;
        private readonly IOptionsMonitor<AiAuditOptions> _auditOptions;

        public TelemetryChatClient(
            IChatClient innerClient,
            IAiAuditService? auditService,
            IBackOfficeSecurityAccessor? securityAccessor,
            IOptionsMonitor<AiAuditOptions> auditOptions)
        {
            _innerClient = innerClient;
            _auditService = auditService;
            _securityAccessor = securityAccessor;
            _auditOptions = auditOptions;
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // ===== PARALLEL INDEPENDENT CONCERNS =====

            // 1. Create Activity (OpenTelemetry - for external observability)
            // Only include safe observability data in Activity tags
            var observabilityTags = new List<KeyValuePair<string, object?>>
            {
                new(AiTelemetrySource.ModelIdTag, options?.ModelId),
                new(AiTelemetrySource.OperationTypeTag, "chat")
            };

            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.ChatRequestActivity,
                ActivityKind.Internal,
                parentContext: default,
                tags: observabilityTags);

            // 2. Start audit recording (INDEPENDENT, with optional Activity correlation)
            Guid? auditId = null;
            if (_auditOptions.CurrentValue.Enabled && _auditService is not null)
            {
                var auditContext = AiAuditContext.ExtractFromOptions(
                    AiCapability.Chat,
                    options,
                    chatMessages.ToList());

                var audit = await _auditService.StartAuditAsync(
                    auditContext,
                    cancellationToken);

                auditId = audit.Id;
            }

            try
            {
                var response = await _innerClient.GetResponseAsync(chatMessages, options, cancellationToken);

                // 3. Update Activity with metrics (if exists)
                if (activity is not null && response.Usage is not null)
                {
                    if (response.Usage.InputTokenCount.HasValue)
                        activity.SetTag(AiTelemetrySource.TokensInputTag, response.Usage.InputTokenCount.Value);
                    if (response.Usage.OutputTokenCount.HasValue)
                        activity.SetTag(AiTelemetrySource.TokensOutputTag, response.Usage.OutputTokenCount.Value);
                    if (response.Usage.TotalTokenCount.HasValue)
                        activity.SetTag(AiTelemetrySource.TokensTotalTag, response.Usage.TotalTokenCount.Value);
                }

                // 4. Complete audit (if exists)
                if (auditId.HasValue && _auditService is not null)
                {
                    await _auditService.CompleteAuditAsync(
                        auditId.Value,
                        response,
                        cancellationToken);
                }

                return response;
            }
            catch (Exception ex)
            {
                // Set Activity error (if exists)
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                    activity.AddEvent(new ActivityEvent("exception",
                        tags: new ActivityTagsCollection
                        {
                            { "exception.type", ex.GetType().Name }
                        }));
                }

                // Record audit failure (if exists)
                if (auditId.HasValue && _auditService is not null)
                {
                    await _auditService.RecordAuditFailureAsync(
                        auditId.Value,
                        ex,
                        cancellationToken);
                }

                throw;
            }
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // ===== PARALLEL INDEPENDENT CONCERNS =====

            // 1. Create Activity (OpenTelemetry - for external observability)
            // Only include safe observability data in Activity tags
            var observabilityTags = new List<KeyValuePair<string, object?>>
            {
                new(AiTelemetrySource.ModelIdTag, options?.ModelId),
                new(AiTelemetrySource.OperationTypeTag, "chat.streaming")
            };

            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.ChatRequestActivity,
                ActivityKind.Internal,
                parentContext: default,
                tags: observabilityTags);

            // 2. Start audit recording (INDEPENDENT, with optional Activity correlation)
            Guid? auditId = null;
            if (_auditOptions.CurrentValue.Enabled && _auditService is not null)
            {
                var auditContext = AiAuditContext.ExtractFromOptions(
                    AiCapability.Chat,
                    options,
                    chatMessages.ToList());

                var audit = await _auditService.StartAuditAsync(
                    auditContext,
                    cancellationToken);   // Pass OTel SpanId for correlation

                auditId = audit.Id;
            }

            IAsyncEnumerable<ChatResponseUpdate> stream;

            try
            {
                stream = _innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
            }
            catch (Exception ex)
            {
                // Set Activity error (if exists)
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                    activity.AddEvent(new ActivityEvent("exception",
                        tags: new ActivityTagsCollection
                        {
                            { "exception.type", ex.GetType().Name }
                        }));
                }

                // Record audit failure (if exists)
                if (auditId.HasValue && _auditService is not null)
                {
                    await _auditService.RecordAuditFailureAsync(
                        auditId.Value,
                        ex,
                        cancellationToken);
                }

                throw;
            }

            // Stream updates - audit completion handled after streaming completes
            await foreach (var update in stream.WithCancellation(cancellationToken))
            {
                yield return update;
            }

            // Mark audit as completed (no response capture for streaming)
            if (auditId.HasValue && _auditService is not null)
            {
                await _auditService.CompleteAuditAsync(
                    auditId.Value,
                    null,  // No complete response for streaming
                    cancellationToken);
            }
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
