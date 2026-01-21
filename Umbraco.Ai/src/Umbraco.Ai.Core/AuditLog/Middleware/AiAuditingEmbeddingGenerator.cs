using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

internal sealed class AiAuditingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    public AiAuditingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiAuditLogService auditLogService,
        IAiAuditLogFactory auditLogFactory,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
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
        AiAuditScope? auditScope = null;
        AiAuditLog? auditLog = null;

        if (_auditLogOptions.CurrentValue.Enabled && _runtimeContextAccessor.Context is not null)
        {
            // Extract audit context from options and values
            var auditLogContext = AiAuditContext.ExtractFromRuntimeContext(
                AiCapability.Embedding,
                _runtimeContextAccessor.Context,
                values.ToList());

            // Extract metadata from options if present
            Dictionary<string, string>? metadata = null;
            if (options?.AdditionalProperties?.TryGetValue(Constants.MetadataKeys.LogKeys, out var logKeys) == true
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
                parentId: AiAuditScope.Current?.AuditLogId); // Capture parent from ambient scope

            // Create scope synchronously (for nested operation tracking)
            auditScope = AiAuditScope.Begin(auditLog.Id);

            // Queue persistence in background (fire-and-forget)
            await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);
        }

        try
        {
            var result = await base.GenerateAsync(values, options, cancellationToken);

            // Complete audit-log (if exists)
            if (auditLog is not null)
            {
                var trackingGenerator = InnerGenerator as AiTrackingEmbeddingGenerator<string, Embedding<float>>;

                // Queue completion in background (fire-and-forget)
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    new AiAuditResponse
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
}