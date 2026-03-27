using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.AuditLog.Middleware;

/// <summary>
/// Speech-to-text middleware that handles audit-log tracking for AI transcription operations.
/// </summary>
internal sealed class AIAuditingSpeechToTextMiddleware : IAISpeechToTextMiddleware
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAuditingSpeechToTextMiddleware"/> class.
    /// </summary>
    public AIAuditingSpeechToTextMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    /// <inheritdoc />
    public ISpeechToTextClient Apply(ISpeechToTextClient client)
    {
        return new AIAuditingSpeechToTextClient(client,
            _runtimeContextAccessor,
            _auditLogService,
            _auditLogFactory,
            _auditLogOptions);
    }
}
