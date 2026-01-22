using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Chat middleware that handles audit-log tracking for AI chat operations.
/// </summary>
internal sealed class AiAuditingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAuditingChatMiddleware"/> class.
    /// </summary>
    public AiAuditingChatMiddleware(
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
    public IChatClient Apply(IChatClient client)
    {
        return new AiAuditingChatClient(client, 
            _runtimeContextAccessor, 
            _auditLogService, 
            _auditLogFactory, 
            _auditLogOptions);
    }
}
