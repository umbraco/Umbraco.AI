namespace Umbraco.AI.Automate;

/// <summary>
/// Constants used throughout Umbraco.AI.Automate.
/// </summary>
public static class UmbracoAIAutomateConstants
{
    /// <summary>
    /// Automate action type aliases for AI operations.
    /// </summary>
    public static class ActionTypes
    {
        /// <summary>
        /// Action alias for running an AI agent.
        /// </summary>
        public const string RunAgent = "umbracoAI.runAgent";
    }

    /// <summary>
    /// Automate trigger type aliases for AI events.
    /// </summary>
    public static class TriggerTypes
    {
        /// <summary>
        /// Trigger alias for when an AI agent completes execution.
        /// </summary>
        public const string AgentExecuted = "umbracoAI.agentExecuted";
    }
}
