using UmbracoAiConstants = Umbraco.Ai.Web.Constants;

namespace Umbraco.Ai.Agent.Web;

/// <summary>
/// Constants for Umbraco AI AiAgent Management API.
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
        public const string ApiName = "ai-agent-management";
        
        /// <summary>
        /// The API name.
        /// </summary>
        public const string ApiTitle = "Umbraco AI Agent Management API";
        
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
            /// AiAgent feature constants.
            /// </summary>
            public static class Agent
            {
                /// <summary>
                /// Route segment for agent endpoints.
                /// </summary>
                public const string RouteSegment = "agents";

                /// <summary>
                /// Swagger group name for agent endpoints.
                /// </summary>
                public const string GroupName = "Agents";
            }
        }
    }
}
