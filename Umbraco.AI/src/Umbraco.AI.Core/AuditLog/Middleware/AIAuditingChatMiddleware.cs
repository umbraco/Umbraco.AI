using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog.Middleware;

/// <summary>
/// Chat middleware that handles audit-log tracking for AI chat operations.
/// </summary>
internal sealed class AIAuditingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiAuditLogService _auditLogService;
    private readonly IAiAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAuditingChatMiddleware"/> class.
    /// </summary>
    public AIAuditingChatMiddleware(
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
    public IChatClient Apply(IChatClient client)
    {
        return new AIAuditingChatClient(client, 
            _runtimeContextAccessor, 
            _auditLogService, 
            _auditLogFactory, 
            _auditLogOptions);
    }
}
