using UmbracoAIConstants = Umbraco.AI.Web.Constants;

namespace Umbraco.AI.Agent.Web;

/// <summary>
/// Constants for Umbraco AI AIAgent Management API.
/// </summary>
public class Constants
{
    /// <summary>
    /// Management API constants - reuses Umbraco.AI shared values.
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
        /// The backoffice API path - shared with Umbraco.AI.
        /// </summary>
        public const string BackofficePath = UmbracoAIConstants.ManagementApi.BackofficePath; // "/ai/management/api"

        /// <summary>
        /// Feature-specific constants.
        /// </summary>
        public static class Feature
        {
            /// <summary>
            /// AIAgent feature constants.
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

            /// <summary>
            /// File storage feature constants.
            /// </summary>
            public static class File
            {
                /// <summary>
                /// Route segment for file endpoints.
                /// </summary>
                public const string RouteSegment = "files";

                /// <summary>
                /// Swagger group name for file endpoints.
                /// </summary>
                public const string GroupName = "Files";
            }

            /// <summary>
            /// AIOrchestration feature constants.
            /// </summary>
            public static class Orchestration
            {
                /// <summary>
                /// Route segment for orchestration endpoints.
                /// </summary>
                public const string RouteSegment = "orchestrations";

                /// <summary>
                /// Swagger group name for orchestration endpoints.
                /// </summary>
                public const string GroupName = "Orchestrations";
            }
        }
    }
}
