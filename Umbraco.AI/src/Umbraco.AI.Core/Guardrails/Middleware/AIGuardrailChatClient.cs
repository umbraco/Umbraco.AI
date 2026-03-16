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

            if (preResult.Action == AIGuardrailAction.Redact)
            {
                var matches = await CollectRedactableMatchesAsync(inputContent, preResult, cancellationToken);
                if (matches.Count > 0)
                {
                    var redactedContent = ApplyRedactions(inputContent, matches);
                    ApplyRedactedContentToUserMessage(messagesList, redactedContent);
                }
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

            if (postResult.Action == AIGuardrailAction.Redact)
            {
                var matches = await CollectRedactableMatchesAsync(responseContent, postResult, cancellationToken);
                if (matches.Count > 0)
                {
                    var redactedContent = ApplyRedactions(responseContent, matches);
                    ApplyRedactedContentToResponse(response, redactedContent);
                }
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

            if (preResult.Action == AIGuardrailAction.Redact)
            {
                var matches = await CollectRedactableMatchesAsync(inputContent, preResult, cancellationToken);
                if (matches.Count > 0)
                {
                    var redactedContent = ApplyRedactions(inputContent, matches);
                    ApplyRedactedContentToUserMessage(messagesList, redactedContent);
                }
            }
        }

        // Phase 2: Stream with code-based post-generate evaluation
        // Note: Post-generate Redact rules degrade to Warn during streaming
        // because already-yielded chunks cannot be retroactively redacted.
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
        var hasRedactAction = false;

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

            if (result.Flagged)
            {
                if (rule.Action == AIGuardrailAction.Block)
                {
                    hasBlockAction = true;
                }
                else if (rule.Action == AIGuardrailAction.Redact)
                {
                    hasRedactAction = true;
                }
            }
        }

        // Precedence: Block > Redact > Warn
        if (hasBlockAction)
        {
            overallAction = AIGuardrailAction.Block;
        }
        else if (hasRedactAction)
        {
            overallAction = AIGuardrailAction.Redact;
        }

        return new AIGuardrailEvaluationResult
        {
            Action = overallAction,
            Phase = phase,
            RuleResults = ruleResults
        };
    }

    private async Task<IReadOnlyList<AIGuardrailRedactableMatch>> CollectRedactableMatchesAsync(
        string content,
        AIGuardrailEvaluationResult evaluationResult,
        CancellationToken cancellationToken)
    {
        var allMatches = new List<AIGuardrailRedactableMatch>();

        foreach (var ruleResult in evaluationResult.RuleResults)
        {
            if (!ruleResult.EvaluatorResult.Flagged || ruleResult.Rule.Action != AIGuardrailAction.Redact)
            {
                continue;
            }

            var evaluator = _evaluators.GetById(ruleResult.Rule.EvaluatorId);
            if (evaluator is not IAIGuardrailRedactable redactable)
            {
                // Evaluator doesn't support redaction — degrades to Warn
                continue;
            }

            var config = new AIGuardrailConfig { Config = ruleResult.Rule.Config };
            var matches = await redactable.FindRedactableMatchesAsync(content, config, cancellationToken);

            allMatches.AddRange(matches);
        }

        return allMatches;
    }

    private static string ApplyRedactions(string content, IReadOnlyList<AIGuardrailRedactableMatch> matches)
    {
        if (matches.Count == 0)
        {
            return content;
        }

        // Merge overlapping ranges
        var sorted = matches.OrderBy(m => m.Index).ThenByDescending(m => m.Length).ToList();
        var merged = new List<(int Index, int Length)>();

        var currentStart = sorted[0].Index;
        var currentEnd = sorted[0].Index + sorted[0].Length;

        for (var i = 1; i < sorted.Count; i++)
        {
            var matchStart = sorted[i].Index;
            var matchEnd = sorted[i].Index + sorted[i].Length;

            if (matchStart <= currentEnd)
            {
                // Overlapping or adjacent — extend
                currentEnd = Math.Max(currentEnd, matchEnd);
            }
            else
            {
                merged.Add((currentStart, currentEnd - currentStart));
                currentStart = matchStart;
                currentEnd = matchEnd;
            }
        }

        merged.Add((currentStart, currentEnd - currentStart));

        // Apply from end to avoid offset shifting
        var sb = new StringBuilder(content);
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            var (index, length) = merged[i];
            sb.Remove(index, length);
            sb.Insert(index, "[REDACTED]");
        }

        return sb.ToString();
    }

    private static void ApplyRedactedContentToUserMessage(List<ChatMessage> messages, string redactedContent)
    {
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage is null)
        {
            return;
        }

        // Replace TextContent items with redacted content
        for (var i = 0; i < lastUserMessage.Contents.Count; i++)
        {
            if (lastUserMessage.Contents[i] is TextContent)
            {
                lastUserMessage.Contents[i] = new TextContent(redactedContent);
                return;
            }
        }
    }

    private static void ApplyRedactedContentToResponse(ChatResponse response, string redactedContent)
    {
        foreach (var message in response.Messages)
        {
            for (var i = 0; i < message.Contents.Count; i++)
            {
                if (message.Contents[i] is TextContent)
                {
                    message.Contents[i] = new TextContent(redactedContent);
                    return;
                }
            }
        }
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
