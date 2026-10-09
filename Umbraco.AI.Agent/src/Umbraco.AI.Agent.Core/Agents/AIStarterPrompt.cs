namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// A single starter prompt shown as a clickable chip in an empty chat, authored on the agent.
/// </summary>
/// <remarks>
/// Kept as an object rather than a bare string so an optional <c>Label</c> can be added later
/// with no data migration.
/// </remarks>
public sealed record AIStarterPrompt
{
    /// <summary>
    /// The prompt text sent when the starter is clicked.
    /// </summary>
    /// <remarks>
    /// Limited to 200 characters, enforced by <see cref="AIAgentService.SaveAgentAsync"/>.
    /// </remarks>
    public required string Prompt { get; init; }
}
