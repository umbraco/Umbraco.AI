using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog.Middleware;

/// <summary>
/// Embedding middleware that handles audit-log tracking for AI embedding operations.
/// </summary>
internal sealed class AIAuditingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAuditingEmbeddingMiddleware"/> class.
    /// </summary>
    public AIAuditingEmbeddingMiddleware(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiAuditLogService auditLogService,
        IAiAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AIAuditingEmbeddingGenerator(generator, 
            _runtimeContextAccessor,
            _auditLogService, 
            _auditLogFactory, 
            _auditLogOptions);
    }
}
