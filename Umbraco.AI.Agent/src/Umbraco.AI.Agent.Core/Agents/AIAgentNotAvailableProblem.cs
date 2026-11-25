namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// The wording of the "Agent not available" refusal, so every endpoint that returns it says the same thing.
/// </summary>
internal static class AIAgentNotAvailableProblem
{
    /// <summary>
    /// The problem title.
    /// </summary>
    public const string Title = "Agent not available";

    /// <summary>
    /// The problem detail.
    /// </summary>
    public const string Detail = "The specified agent is not available to you.";
}
