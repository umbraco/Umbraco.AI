using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog.Middleware;

internal sealed class AIAuditingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
        : base(innerGenerator)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Start audit-log recording if enabled
        AIAuditScope? auditScope = null;
        AIAuditLog? auditLog = null;

        if (_auditLogOptions.CurrentValue.Enabled && _runtimeContextAccessor.Context is not null)
        {
            // Extract audit context from options and values
            var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
                AICapability.Embedding,
                _runtimeContextAccessor.Context,
                values.ToList());

            // Extract metadata from options if present
            Dictionary<string, string>? metadata = null;
            if (options?.AdditionalProperties?.TryGetValue(Constants.ContextKeys.LogKeys, out var logKeys) == true
                && logKeys is IEnumerable<string> keys)
            {
                metadata = keys.ToDictionary(
                    key => key,
                    key => options?.AdditionalProperties?[key]?.ToString() ?? string.Empty);
            }

            // Create audit-log entry using factory
            auditLog = _auditLogFactory.Create(
                auditLogContext,
                metadata,
                parentId: AIAuditScope.Current?.AuditLogId);

            auditScope = AIAuditScope.Begin(auditLog.Id);

            // Capture TraceId from ambient Activity (created by OpenTelemetry middleware)
            auditLog.TraceId = Activity.Current?.TraceId.ToString();

            await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);
        }

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _runtimeContextAccessor);

        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = values.ToList(), Capability = AICapability.Embedding }
            : null;

        try
        {
            var result = await base.GenerateAsync(values, options, cancellationToken);

            if (auditLog is not null)
            {
                var trackingGenerator = InnerGenerator as AITrackingEmbeddingGenerator<string, Embedding<float>>;

                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    auditPrompt,
                    new AIAuditResponse
                    {
                        Usage = trackingGenerator?.LastUsageDetails,
                        Data = trackingGenerator?.LastEmbeddings
                    },
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (auditLog is not null)
            {
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, cancellationToken);
            }

            throw;
        }
        finally
        {
            auditScope?.Dispose();
        }
    }
}
