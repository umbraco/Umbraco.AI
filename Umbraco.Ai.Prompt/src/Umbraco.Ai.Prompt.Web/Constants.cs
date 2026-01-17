using UmbracoAiConstants = Umbraco.Ai.Web.Constants;

namespace Umbraco.Ai.Prompt.Web;

/// <summary>
/// Constants for Umbraco AI AiPrompt Management API.
/// </summary>
public class Constants
{
    /// <summary>
    /// Management API constants - reuses Umbraco.Ai shared values.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>
        /// The API name.
        /// </summary>
        public const string ApiName = "ai-prompt-management";
        
        /// <summary>
        /// The API name.
        /// </summary>
        public const string ApiTitle = "Umbraco AI Prompt Management API";
        
        /// <summary>
        /// The backoffice API path - shared with Umbraco.Ai.
        /// </summary>
        public const string BackofficePath = UmbracoAiConstants.ManagementApi.BackofficePath; // "/ai/management/api"

        /// <summary>
        /// Feature-specific constants.
        /// </summary>
        public static class Feature
        {
            /// <summary>
            /// AiPrompt feature constants.
            /// </summary>
            public static class Prompt
            {
                /// <summary>
                /// Route segment for prompt endpoints.
                /// </summary>
                public const string RouteSegment = "prompts";

                /// <summary>
                /// Swagger group name for prompt endpoints.
                /// </summary>
                public const string GroupName = "Prompts";
            }

            /// <summary>
            /// Utils feature constants.
            /// </summary>
            public static class Utils
            {
                /// <summary>
                /// Route segment for utils endpoints.
                /// </summary>
                public const string RouteSegment = "utils";

                /// <summary>
                /// Swagger group name for utils endpoints.
                /// </summary>
                public const string GroupName = "Utils";
            }
        }
    }
}
