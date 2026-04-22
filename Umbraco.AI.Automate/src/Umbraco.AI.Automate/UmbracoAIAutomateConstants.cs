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

        /// <summary>
        /// Action alias for transcribing audio using an AI speech-to-text profile.
        /// </summary>
        public const string TranscribeAudio = "umbracoAI.transcribeAudio";
    }

    /// <summary>
    /// Automate trigger type aliases for AI operations.
    /// </summary>
    public static class TriggerTypes
    {
        /// <summary>
        /// Trigger alias for the AI agent trigger.
        /// </summary>
        public const string AgentTrigger = "umbracoAI.agentTrigger";

        /// <summary>
        /// Trigger alias for the AI agent run completed trigger.
        /// </summary>
        public const string AgentRunCompleted = "umbracoAI.agentRunCompleted";

        /// <summary>
        /// Trigger alias for the AI agent run failed trigger.
        /// </summary>
        public const string AgentRunFailed = "umbracoAI.agentRunFailed";
    }

    /// <summary>
    /// Trigger aliases that are allowed to be invoked by AI agent tools.
    /// </summary>
    public static readonly IReadOnlyList<string> AgentInvokableTriggerAliases =
    [
        "umbracoAutomate.manual",
        TriggerTypes.AgentTrigger,
    ];
}
