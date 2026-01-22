using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Embedding middleware that handles audit-log tracking for AI embedding operations.
/// </summary>
public sealed class AiAuditingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAuditingEmbeddingMiddleware"/> class.
    /// </summary>
    public AiAuditingEmbeddingMiddleware(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiAuditLogService auditLogService,
        IAiAuditLogFactory auditLogFactory,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AiAuditingEmbeddingGenerator(generator, 
            _runtimeContextAccessor,
            _auditLogService, 
            _auditLogFactory, 
            _auditLogOptions);
    }
}
