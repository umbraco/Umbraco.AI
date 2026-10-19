using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Suggests starter prompts for an agent by asking the chat model to draft candidates from the
/// agent's own <c>Instructions</c>. A single structured-output chat call, no agent run — the
/// result is never persisted, it only drafts rows for the author to accept, edit or discard.
/// </summary>
public interface IAIStarterPromptSuggester
{
    /// <summary>
    /// Suggests up to <see cref="AIStarterPromptSuggester.MaxStarters"/> starter prompts for the
    /// given agent, clamped to <see cref="AIStarterPromptSuggester.MaxStarterLength"/> characters each.
    /// </summary>
    /// <param name="agent">The agent to suggest starters for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggested starter prompts.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no profile can be resolved (neither the agent's own nor a default chat profile),
    /// or when the model returned nothing usable.
    /// </exception>
    Task<IReadOnlyList<string>> SuggestStartersAsync(AIAgent agent, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IAIStarterPromptSuggester"/>
internal sealed class AIStarterPromptSuggester : IAIStarterPromptSuggester
{
    /// <summary>
    /// Maximum number of starters returned, regardless of how many the model suggests.
    /// Mirrors <c>AIAgentService.MaxStarterPrompts</c> — the two are not shared because clamping
    /// here is a defensive backstop against the model, not the authored-content guard save enforces.
    /// </summary>
    internal const int MaxStarters = 4;

    /// <summary>
    /// Maximum length, in characters, of a single suggested starter. Mirrors
    /// <c>AIAgentService.MaxStarterPromptLength</c> for the same reason as <see cref="MaxStarters"/>.
    /// </summary>
    internal const int MaxStarterLength = 200;

    private readonly IAIChatService _chatService;

    public AIStarterPromptSuggester(IAIChatService chatService)
    {
        _chatService = chatService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SuggestStartersAsync(AIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var instructions = agent.GetStandardConfig()?.Instructions;

        var userPrompt = $"""
            Suggest up to {MaxStarters} short starter prompts — example first messages a user could
            click to begin a conversation with an AI agent that has the following instructions.

            Rules:
            - Each suggestion must read as a complete, natural message from the user, not a title or label.
            - Keep each suggestion under {MaxStarterLength} characters.
            - Do not number the suggestions or add any other formatting.
            - Suggestions should reflect what this specific agent can help with.

            Agent instructions:
            {(string.IsNullOrWhiteSpace(instructions) ? "(none provided)" : instructions)}
            """;

        List<ChatMessage> messages = [new(ChatRole.User, userPrompt)];

        var response = await _chatService.GetChatResponseAsync(chat =>
        {
            chat.WithAlias("agent-suggest-starters");
            if (agent.ProfileId.HasValue)
            {
                chat.WithProfile(agent.ProfileId.Value);
            }

            chat.WithOutputSchema(AIOutputSchema.FromType<SuggestedStartersResponse>());
        }, messages, cancellationToken);

        if (!response.TryGetResult<SuggestedStartersResponse>(out var parsed))
        {
            throw new InvalidOperationException("The AI model did not return a usable set of starter prompt suggestions.");
        }

        var starters = parsed.Starters
            .Where(starter => !string.IsNullOrWhiteSpace(starter))
            .Select(starter => starter.Trim())
            .Select(starter => starter.Length > MaxStarterLength ? starter[..MaxStarterLength] : starter)
            .Take(MaxStarters)
            .ToList();

        if (starters.Count == 0)
        {
            throw new InvalidOperationException("The AI model did not return any usable starter prompt suggestions.");
        }

        return starters;
    }
}

/// <summary>
/// Structured response schema for <see cref="AIStarterPromptSuggester"/>.
/// </summary>
internal sealed record SuggestedStartersResponse
{
    public required IReadOnlyList<string> Starters { get; init; }
}
