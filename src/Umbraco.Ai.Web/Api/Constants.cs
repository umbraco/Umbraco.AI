namespace Umbraco.Ai.Web.Api;

/// <summary>
/// Defines constants for the Umbraco AI Management API.
/// </summary>
public class Constants
{
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
        public const string ApiNamespacePrefix = "Umbraco.Ai.Web.Api.Management";

        /// <summary>
        /// The name of the API group for version 1.0.
        /// </summary>
        public const string GroupNameV1 = "1.0";
        
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
                public const string RouteSegment = "embedding";

                /// <summary>
                /// The Swagger group name for Embedding features.
                /// </summary>
                public const string GroupName = "Embedding";
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
        }
    }
}