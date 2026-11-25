namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Saves prompts without the user group change rule that <see cref="IAIPromptService"/> applies to the signed-in
/// user. For Deploy import only: an import always saves the exported user groups, whoever started it. Every
/// other validation still runs. Never use it on a request path: use <see cref="IAIPromptService"/> there.
/// </summary>
internal interface IAIPromptUnrestrictedWriter
{
    /// <summary>
    /// Saves a prompt, skipping the check on who may change its user groups.
    /// </summary>
    Task<AIPrompt> SavePromptUnrestrictedAsync(AIPrompt prompt, CancellationToken cancellationToken = default);
}
