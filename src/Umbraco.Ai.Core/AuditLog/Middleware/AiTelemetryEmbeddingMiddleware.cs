using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Embedding middleware that handles audit-log tracking for AI embedding operations.
/// </summary>
public sealed class AiTelemetryEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiTelemetryEmbeddingMiddleware"/> class.
    /// </summary>
    public AiTelemetryEmbeddingMiddleware(
        IAiAuditLogService auditLogService,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
    {
        _auditLogService = auditLogService;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new TelemetryEmbeddingGenerator(generator, _auditLogService, _auditLogOptions);
    }

    private sealed class TelemetryEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly IAiAuditLogService _auditLogService;
        private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

        public TelemetryEmbeddingGenerator(
            IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
            IAiAuditLogService auditLogService,
            IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
            : base(innerGenerator)
        {
            _auditLogService = auditLogService;
            _auditLogOptions = auditLogOptions;
        }

        public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Start audit-log recording if enabled
            AiAuditScopeHandle? auditLogHandle = null;
            if (_auditLogOptions.CurrentValue.Enabled)
            {
                var auditLogContext = AiAuditContext.ExtractFromOptions(
                    AiCapability.Embedding,
                    options,
                    values.ToList());

                auditLogHandle = await _auditLogService.StartAuditLogScopeAsync(
                    auditLogContext,
                    ct: cancellationToken);
            }

            try
            {
                var result = await base.GenerateAsync(values, options, cancellationToken);

                // Complete audit-log (if exists)
                if (auditLogHandle is not null)
                {
                    await _auditLogService.CompleteAuditLogAsync(
                        auditLogHandle.AuditLog,
                        result,
                        cancellationToken);
                }

                return result;
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
    }
}
