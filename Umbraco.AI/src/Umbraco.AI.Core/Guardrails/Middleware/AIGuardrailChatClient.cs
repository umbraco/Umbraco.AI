using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Middleware;

/// <summary>
/// A delegating chat client that enforces guardrails on inputs and responses.
/// </summary>
internal sealed class AIGuardrailChatClient : DelegatingChatClient
{
    private const int SlidingWindowSize = 100;

    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailResolutionService _resolutionService;
    private readonly AIGuardrailEvaluatorCollection _evaluators;

    public AIGuardrailChatClient(
        IChatClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailResolutionService resolutionService,
        AIGuardrailEvaluatorCollection evaluators)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _resolutionService = resolutionService;
        _evaluators = evaluators;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Skip if this is a guardrail evaluation call (prevent infinite recursion)
        if (IsGuardrailEvaluation())
        {
            return await InnerClient.GetResponseAsync(chatMessages, options, cancellationToken);
        }

        var messagesList = chatMessages.ToList();

        // Resolve applicable guardrails
        var resolved = await _resolutionService.ResolveGuardrailsAsync(cancellationToken);
        if (!resolved.HasRules)
        {
            return await InnerClient.GetResponseAsync(messagesList, options, cancellationToken);
        }

        // Phase 1: Pre-generate evaluation
        if (resolved.PreGenerateRules.Count > 0)
        {
            var inputContent = ExtractUserContent(messagesList);
            var preResult = await EvaluateRulesAsync(
                inputContent, messagesList, resolved.PreGenerateRules, AIGuardrailPhase.PreGenerate, cancellationToken);

            if (preResult.Action == AIGuardrailAction.Block)
            {
                throw new AIGuardrailBlockedException(preResult);
            }
        }

        // Execute the actual AI call
        var response = await InnerClient.GetResponseAsync(messagesList, options, cancellationToken);

        // Phase 2: Post-generate evaluation
        if (resolved.PostGenerateRules.Count > 0)
        {
            var responseContent = ExtractResponseContent(response);
            var postResult = await EvaluateRulesAsync(
                responseContent, messagesList, resolved.PostGenerateRules, AIGuardrailPhase.PostGenerate, cancellationToken);

            if (postResult.Action == AIGuardrailAction.Block)
            {
                throw new AIGuardrailBlockedException(postResult);
            }
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Skip if this is a guardrail evaluation call (prevent infinite recursion)
        if (IsGuardrailEvaluation())
        {
            await foreach (var update in InnerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        var messagesList = chatMessages.ToList();

        // Resolve applicable guardrails
        var resolved = await _resolutionService.ResolveGuardrailsAsync(cancellationToken);
        if (!resolved.HasRules)
        {
            await foreach (var update in InnerClient.GetStreamingResponseAsync(messagesList, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // Phase 1: Pre-generate evaluation
        if (resolved.PreGenerateRules.Count > 0)
        {
            var inputContent = ExtractUserContent(messagesList);
            var preResult = await EvaluateRulesAsync(
                inputContent, messagesList, resolved.PreGenerateRules, AIGuardrailPhase.PreGenerate, cancellationToken);

            if (preResult.Action == AIGuardrailAction.Block)
            {
                throw new AIGuardrailBlockedException(preResult);
            }
        }

        // Phase 2: Stream with code-based post-generate evaluation
        var codeBasedPostRules = resolved.PostGenerateRules
            .Where(r => _evaluators.GetById(r.EvaluatorId)?.Type == AIGuardrailEvaluatorType.CodeBased)
            .ToList();
        var modelBasedPostRules = resolved.PostGenerateRules
            .Where(r => _evaluators.GetById(r.EvaluatorId)?.Type == AIGuardrailEvaluatorType.ModelBased)
            .ToList();

        var slidingWindow = new StringBuilder();
        var fullContent = new StringBuilder();
        var bufferedUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in InnerClient.GetStreamingResponseAsync(messagesList, options, cancellationToken))
        {
            var text = update.Text;
            if (!string.IsNullOrEmpty(text))
            {
                fullContent.Append(text);
                slidingWindow.Append(text);

                // Evaluate code-based rules on sliding window
                if (codeBasedPostRules.Count > 0)
                {
                    var windowContent = slidingWindow.ToString();
                    var chunkResult = await EvaluateRulesAsync(
                        windowContent, messagesList, codeBasedPostRules, AIGuardrailPhase.PostGenerate, cancellationToken);

                    if (chunkResult.Action == AIGuardrailAction.Block)
                    {
                        throw new AIGuardrailBlockedException(chunkResult);
                    }

                    // Trim sliding window to keep it bounded
                    if (slidingWindow.Length > SlidingWindowSize * 2)
                    {
                        slidingWindow.Remove(0, slidingWindow.Length - SlidingWindowSize);
                    }
                }
            }

            // If we have model-based rules, buffer updates for potential post-stream evaluation
            if (modelBasedPostRules.Count > 0)
            {
                bufferedUpdates.Add(update);
            }

            yield return update;
        }

        // Phase 3: Post-stream model-based evaluation on full content
        if (modelBasedPostRules.Count > 0 && fullContent.Length > 0)
        {
            var postResult = await EvaluateRulesAsync(
                fullContent.ToString(), messagesList, modelBasedPostRules, AIGuardrailPhase.PostGenerate, cancellationToken);

            if (postResult.Action == AIGuardrailAction.Block)
            {
                throw new AIGuardrailBlockedException(postResult);
            }
        }
    }

    private bool IsGuardrailEvaluation()
    {
        return _runtimeContextAccessor.Context?.TryGetValue<bool>(
            Constants.ContextKeys.IsGuardrailEvaluation, out var isEval) == true && isEval;
    }

    private async Task<AIGuardrailEvaluationResult> EvaluateRulesAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        IReadOnlyList<AIGuardrailRule> rules,
        AIGuardrailPhase phase,
        CancellationToken cancellationToken)
    {
        var ruleResults = new List<AIGuardrailRuleResult>();
        var overallAction = AIGuardrailAction.Warn; // Default to most permissive
        var hasBlockAction = false;

        foreach (var rule in rules)
        {
            var evaluator = _evaluators.GetById(rule.EvaluatorId);
            if (evaluator is null)
            {
                continue;
            }

            var config = new AIGuardrailConfig { Config = rule.Config };
            var result = await evaluator.EvaluateAsync(content, conversationHistory, config, cancellationToken);

            ruleResults.Add(new AIGuardrailRuleResult
            {
                Rule = rule,
                EvaluatorResult = result
            });

            if (result.Flagged && rule.Action == AIGuardrailAction.Block)
            {
                hasBlockAction = true;
            }
        }

        if (hasBlockAction)
        {
            overallAction = AIGuardrailAction.Block;
        }

        return new AIGuardrailEvaluationResult
        {
            Action = overallAction,
            Phase = phase,
            RuleResults = ruleResults
        };
    }

    private static string ExtractUserContent(IReadOnlyList<ChatMessage> messages)
    {
        // Extract the last user message content for pre-generate evaluation
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        return lastUserMessage?.Text ?? string.Empty;
    }

    private static string ExtractResponseContent(ChatResponse response)
    {
        return response.Text ?? string.Empty;
    }
}
