using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

internal sealed class AiAuditingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    public AiAuditingEmbeddingGenerator(
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
            
            Dictionary<string, string>? metadata = null;
            var logKeys = options?.AdditionalProperties?[Constants.MetadataKeys.LogKeys];
            if (logKeys is IEnumerable<string> keys)
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
            var result = await base.GenerateAsync(values, options, cancellationToken);

            // Complete audit-log (if exists)
            if (auditLogHandle is not null)
            {
                var trackingChatClient = InnerGenerator as AiTrackingEmbeddingGenerator<string, Embedding<float>>;
                
                await _auditLogService.CompleteAuditLogAsync(
                    auditLogHandle.AuditLog,
                    new AiAuditResponse
                    {
                        Usage = trackingChatClient?.LastUsageDetails,
                        Data = trackingChatClient?.LastEmbeddings
                    },
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