using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Embedding middleware that handles audit-log tracking for AI embedding operations.
/// </summary>
public sealed class AiAuditingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAuditingEmbeddingMiddleware"/> class.
    /// </summary>
    public AiAuditingEmbeddingMiddleware(
        IAiAuditLogService auditLogService,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
    {
        _auditLogService = auditLogService;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AiAuditingEmbeddingGenerator(generator, _auditLogService, _auditLogOptions);
    }
}
