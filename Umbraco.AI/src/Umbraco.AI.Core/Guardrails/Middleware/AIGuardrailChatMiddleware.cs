using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Middleware;

/// <summary>
/// Chat middleware that enforces guardrails on AI inputs and responses.
/// </summary>
/// <remarks>
/// This middleware resolves applicable guardrails from all registered resolvers and evaluates
/// content in two phases:
/// <list type="bullet">
/// <item>PreGenerate: evaluates user input before sending to the AI provider</item>
/// <item>PostGenerate: evaluates the AI response before returning to the caller</item>
/// </list>
/// </remarks>
public sealed class AIGuardrailChatMiddleware : IAIChatMiddleware
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailResolutionService _resolutionService;
    private readonly AIGuardrailEvaluatorCollection _evaluators;
    private readonly ILogger<AIGuardrailChatMiddleware> _logger;

    public AIGuardrailChatMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailResolutionService resolutionService,
        AIGuardrailEvaluatorCollection evaluators,
        ILogger<AIGuardrailChatMiddleware> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _resolutionService = resolutionService;
        _evaluators = evaluators;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIGuardrailChatClient(
            client,
            _runtimeContextAccessor,
            _resolutionService,
            _evaluators,
            _logger);
    }
}
