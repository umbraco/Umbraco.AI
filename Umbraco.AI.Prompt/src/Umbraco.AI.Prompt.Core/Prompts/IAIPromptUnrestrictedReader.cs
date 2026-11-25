namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Reads prompts without the per-user limit that <see cref="IAIPromptService"/> applies. For server-side code
/// that acts on a prompt after the execution was already authorized (resolvers, telemetry), or that has no
/// user at all (Deploy). Never use it on a request path: use <see cref="IAIPromptService"/> there.
/// </summary>
internal interface IAIPromptUnrestrictedReader
{
    /// <summary>
    /// Gets a prompt by ID, whoever may use it.
    /// </summary>
    Task<AIPrompt?> GetPromptUnrestrictedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all prompts, whoever may use them.
    /// </summary>
    Task<IEnumerable<AIPrompt>> GetPromptsUnrestrictedAsync(CancellationToken cancellationToken = default);
}
