using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;

namespace Umbraco.Ai.Core.AuditLog.Middleware;

/// <summary>
/// Chat middleware that handles audit-log tracking for AI chat operations.
/// </summary>
public sealed class AiAuditingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IOptionsMonitor<AiAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAuditingChatMiddleware"/> class.
    /// </summary>
    public AiAuditingChatMiddleware(
        IAiAuditLogService auditLogService,
        IOptionsMonitor<AiAuditLogOptions> auditLogOptions)
    {
        _auditLogService = auditLogService;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AiAuditingChatClient(client, _auditLogService, _auditLogOptions);
    }
}
