using UmbracoAiConstants = Umbraco.Ai.Web.Api.Constants;

namespace Umbraco.Ai.Prompt.Web.Api;

/// <summary>
/// Constants for Umbraco AI Prompt Management API.
/// </summary>
public class Constants
{
    /// <summary>
    /// Management API constants - reuses Umbraco.Ai shared values.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>
        /// The API name - shared with Umbraco.Ai for same Swagger group.
        /// </summary>
        public const string ApiName = UmbracoAiConstants.ManagementApi.ApiName; // "ai-management"

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
            /// Prompt feature constants.
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
        }
    }
}
