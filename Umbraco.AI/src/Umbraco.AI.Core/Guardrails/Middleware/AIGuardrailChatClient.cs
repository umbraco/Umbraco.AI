using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;

    public AIGuardrailChatClient(
        IChatClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailResolutionService resolutionService,
        AIGuardrailEvaluatorCollection evaluators,
        ILogger logger)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _resolutionService = resolutionService;
        _evaluators = evaluators;
        _logger = logger;
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
                var matches = await CollectRedactionCandidateesAsync(inputContent, preResult, cancellationToken);
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
                var matches = await CollectRedactionCandidateesAsync(responseContent, postResult, cancellationToken);
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
                var matches = await CollectRedactionCandidateesAsync(inputContent, preResult, cancellationToken);
                if (matches.Count > 0)
                {
                    var redactedContent = ApplyRedactions(inputContent, matches);
                    ApplyRedactedContentToUserMessage(messagesList, redactedContent);
                }
            }
        }

        // Phase 2: Stream with code-based post-generate evaluation.
        // Text isn't released to the caller as soon as it arrives — the trailing SlidingWindowSize
        // characters are always held back first, so a match that completes shortly after it first
        // appears can still be caught (Block) or scrubbed (Redact) before it's ever yielded. Only
        // text older than that trailing window is safe to release. A match that never fully fits
        // inside the window can still slip through partially exposed — an inherent limit of scanning
        // a live stream rather than a completed response.
        var codeBasedPostRules = resolved.PostGenerateRules
            .Where(r => _evaluators.GetById(r.EvaluatorId)?.Type == AIGuardrailEvaluatorType.CodeBased)
            .ToList();
        var modelBasedPostRules = resolved.PostGenerateRules
            .Where(r => _evaluators.GetById(r.EvaluatorId)?.Type == AIGuardrailEvaluatorType.ModelBased)
            .ToList();

        var pendingText = new StringBuilder();
        var fullContent = new StringBuilder();
        ChatResponseUpdate? lastUpdate = null;

        await foreach (var update in InnerClient.GetStreamingResponseAsync(messagesList, options, cancellationToken))
        {
            lastUpdate = update;

            if (codeBasedPostRules.Count == 0)
            {
                if (modelBasedPostRules.Count > 0 && !string.IsNullOrEmpty(update.Text))
                {
                    fullContent.Append(update.Text);
                }

                yield return update;
                continue;
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                fullContent.Append(update.Text);
                pendingText.Append(update.Text);
            }

            // A boundary (non-text content, e.g. a tool call, or the terminating update) can't be
            // held back like ordinary text deltas, so release everything still pending up to it now.
            var isBoundary = update.FinishReason is not null || update.Contents.Any(c => c is not TextContent);

            var released = await ReleasePendingTextAsync(
                pendingText, codeBasedPostRules, messagesList, flushAll: isBoundary, cancellationToken);

            if (released.Length > 0)
            {
                yield return CreateTextOnlyUpdate(update, released);
            }

            if (isBoundary)
            {
                yield return update.Contents.Any(c => c is TextContent)
                    ? CreateWithoutTextContent(update)
                    : update;
            }
        }

        // Safety net: release anything still held back if the stream ended without a boundary
        // update (e.g. FinishReason was never set) so no trailing text is silently dropped.
        if (pendingText.Length > 0)
        {
            var released = await ReleasePendingTextAsync(
                pendingText, codeBasedPostRules, messagesList, flushAll: true, cancellationToken);

            if (released.Length > 0)
            {
                yield return CreateTextOnlyUpdate(lastUpdate ?? new ChatResponseUpdate(ChatRole.Assistant, string.Empty), released);
            }
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

    /// <summary>
    /// Evaluates code-based post-generate rules against everything currently held back, applies
    /// redactions to the held-back buffer in place, then releases whatever is now safe to emit —
    /// everything when <paramref name="flushAll"/> is set, otherwise everything except the trailing
    /// <see cref="SlidingWindowSize"/> characters, which stay buffered as lookback for the next chunk.
    /// </summary>
    private async Task<string> ReleasePendingTextAsync(
        StringBuilder pendingText,
        IReadOnlyList<AIGuardrailRule> codeBasedPostRules,
        IReadOnlyList<ChatMessage> conversationHistory,
        bool flushAll,
        CancellationToken cancellationToken)
    {
        if (pendingText.Length == 0)
        {
            return string.Empty;
        }

        var bufferedContent = pendingText.ToString();
        var result = await EvaluateRulesAsync(
            bufferedContent, conversationHistory, codeBasedPostRules, AIGuardrailPhase.PostGenerate, cancellationToken);

        if (result.Action == AIGuardrailAction.Block)
        {
            throw new AIGuardrailBlockedException(result);
        }

        if (result.Action == AIGuardrailAction.Redact)
        {
            var matches = await CollectRedactionCandidateesAsync(bufferedContent, result, cancellationToken);
            if (matches.Count > 0)
            {
                bufferedContent = ApplyRedactions(bufferedContent, matches);
                pendingText.Clear();
                pendingText.Append(bufferedContent);
            }
        }

        var releaseLength = flushAll
            ? pendingText.Length
            : Math.Max(0, pendingText.Length - SlidingWindowSize);

        if (releaseLength == 0)
        {
            return string.Empty;
        }

        var released = pendingText.ToString(0, releaseLength);
        pendingText.Remove(0, releaseLength);
        return released;
    }

    /// <summary>
    /// Clones an update's metadata (role, ids, model, etc.) but replaces its contents with a single
    /// text block — used to emit text that was held back and released on a later update than the one
    /// that originally produced it.
    /// </summary>
    private static ChatResponseUpdate CreateTextOnlyUpdate(ChatResponseUpdate source, string text)
    {
        var clone = source.Clone();
        clone.Contents = [new TextContent(text)];
        return clone;
    }

    /// <summary>
    /// Clones a boundary update with its <see cref="TextContent"/> items removed, since that text was
    /// already released separately — leaves non-text content (e.g. function calls) and metadata (e.g.
    /// FinishReason) intact.
    /// </summary>
    private static ChatResponseUpdate CreateWithoutTextContent(ChatResponseUpdate source)
    {
        var clone = source.Clone();
        clone.Contents = source.Contents.Where(c => c is not TextContent).ToList();
        return clone;
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
                _logger.LogWarning(
                    "Guardrail rule '{RuleName}' (evaluator: {EvaluatorId}) flagged content during {Phase} with action {Action}. Reason: {Reason}",
                    rule.Name,
                    rule.EvaluatorId,
                    phase,
                    rule.Action,
                    result.Reason ?? "No reason provided");

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

    private async Task<IReadOnlyList<AIGuardrailRedactionCandidate>> CollectRedactionCandidateesAsync(
        string content,
        AIGuardrailEvaluationResult evaluationResult,
        CancellationToken cancellationToken)
    {
        var allMatches = new List<AIGuardrailRedactionCandidate>();

        foreach (var ruleResult in evaluationResult.RuleResults)
        {
            if (!ruleResult.EvaluatorResult.Flagged || ruleResult.Rule.Action != AIGuardrailAction.Redact)
            {
                continue;
            }

            var evaluator = _evaluators.GetById(ruleResult.Rule.EvaluatorId);
            if (evaluator is not IAIRedactableGuardrailEvaluator redactable)
            {
                // Evaluator doesn't support redaction — degrades to Warn
                continue;
            }

            var config = new AIGuardrailConfig { Config = ruleResult.Rule.Config };
            var candidates = await redactable.FindRedactionCandidatesAsync(content, config, cancellationToken);

            allMatches.AddRange(candidates);
        }

        return allMatches;
    }

    private static string ApplyRedactions(string content, IReadOnlyList<AIGuardrailRedactionCandidate> matches)
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
