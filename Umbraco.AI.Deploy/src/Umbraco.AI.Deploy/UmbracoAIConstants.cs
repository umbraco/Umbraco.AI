namespace Umbraco.AI.Deploy;

/// <summary>
/// Constants used throughout Umbraco.AI Deploy.
/// </summary>
public static class UmbracoAIConstants
{
    /// <summary>
    /// UDI entity type identifiers for Umbraco.AI entities.
    /// </summary>
    public static class UdiEntityType
    {
        /// <summary>
        /// UDI entity type for AI contexts.
        /// </summary>
        public const string Context = "umbraco-ai-context";

        /// <summary>
        /// UDI entity type for AI connections.
        /// </summary>
        public const string Connection = "umbraco-ai-connection";

        /// <summary>
        /// UDI entity type for AI profiles.
        /// </summary>
        public const string Profile = "umbraco-ai-profile";

        /// <summary>
        /// UDI entity type for AI settings.
        /// </summary>
        public const string Settings = "umbraco-ai-settings";
    }
}
