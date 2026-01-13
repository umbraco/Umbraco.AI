namespace Umbraco.Ai.Web;

/// <summary>
/// Defines constants for the Umbraco AI Management API.
/// </summary>
public class Constants
{
    /// <summary>
    /// Defines the root namespace for the application.
    /// </summary>
    public const string AppNamespaceRoot = "Umbraco.Ai";
    
    /// <summary>
    /// Defines constants for the AI Management API.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>
        /// The API root path.
        /// </summary>
        public const string BackofficePath = "/ai/management/api";

        /// <summary>
        /// The API title.
        /// </summary>

        public const string ApiTitle = "Umbraco AI Management API";

        /// <summary>
        /// The API name.
        /// </summary>
        public const string ApiName = "ai-management";

        /// <summary>
        /// The namespace prefix for AI Management API.
        /// </summary>
        public const string ApiNamespacePrefix = $"{AppNamespaceRoot}.Web.Api.Management";
        
        /// <summary>
        /// Defines constants for different feature areas within the Management API.
        /// </summary>
        public static class Feature
        {
            /// <summary>
            /// Defines constants for Connection features.
            /// </summary>
            public static class Connection
            {
                /// <summary>
                /// The route segment for Connection features.
                /// </summary>
                public const string RouteSegment = "connections";

                /// <summary>
                /// The Swagger group name for Connection features.
                /// </summary>
                public const string GroupName = "Connections";
            }

            /// <summary>
            /// Defines constants for Profile features.
            /// </summary>
            public static class Profile
            {
                /// <summary>
                /// The route segment for Profile features.
                /// </summary>
                public const string RouteSegment = "profiles";

                /// <summary>
                /// The Swagger group name for Profile features.
                /// </summary>
                public const string GroupName = "Profiles";
            }

            /// <summary>
            /// Defines constants for Provider features.
            /// </summary>
            public static class Provider
            {
                /// <summary>
                /// The route segment for Provider features.
                /// </summary>
                public const string RouteSegment = "providers";

                /// <summary>
                /// The Swagger group name for Provider features.
                /// </summary>
                public const string GroupName = "Providers";
            }

            /// <summary>
            /// Defines constants for Embedding features.
            /// </summary>
            public static class Embedding
            {
                /// <summary>
                /// The route segment for Embedding features.
                /// </summary>
                public const string RouteSegment = "embeddings";

                /// <summary>
                /// The Swagger group name for Embedding features.
                /// </summary>
                public const string GroupName = "Embeddings";
            }

            /// <summary>
            /// Defines constants for Chat features.
            /// </summary>
            public static class Chat
            {
                /// <summary>
                /// The route segment for Chat features.
                /// </summary>
                public const string RouteSegment = "chat";

                /// <summary>
                /// The Swagger group name for Chat features.
                /// </summary>
                public const string GroupName = "Chat";
            }

            /// <summary>
            /// Defines constants for Context features.
            /// </summary>
            public static class Context
            {
                /// <summary>
                /// The route segment for Context features.
                /// </summary>
                public const string RouteSegment = "contexts";

                /// <summary>
                /// The Swagger group name for Context features.
                /// </summary>
                public const string GroupName = "Contexts";
            }

            /// <summary>
            /// Defines constants for Resource Type features.
            /// </summary>
            public static class ContextResourceTypes
            {
                /// <summary>
                /// The route segment for Context features.
                /// </summary>
                public const string RouteSegment = "context-resource-types";

                /// <summary>
                /// The Swagger group name for Context features.
                /// </summary>
                public const string GroupName = "Context Resource Types";
            }

            /// <summary>
            /// Defines constants for Audit features.
            /// </summary>
            public static class Audit
            {
                /// <summary>
                /// The route segment for Audit features.
                /// </summary>
                public const string RouteSegment = "audits";

                /// <summary>
                /// The Swagger group name for Audit features.
                /// </summary>
                public const string GroupName = "Audits";
            }
        }
    }
}
