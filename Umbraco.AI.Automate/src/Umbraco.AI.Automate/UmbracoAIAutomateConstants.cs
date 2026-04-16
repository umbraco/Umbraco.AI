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
}
