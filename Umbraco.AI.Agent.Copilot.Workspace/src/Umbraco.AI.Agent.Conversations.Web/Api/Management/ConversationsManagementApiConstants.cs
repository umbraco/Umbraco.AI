namespace Umbraco.AI.Agent.Conversations.Web.Api.Management;

/// <summary>
/// Route segments and OpenAPI group names for the Conversations and Projects management controllers.
/// Deliberately free of any host (product) naming: the hosting product binds these controllers to its
/// own OpenAPI document, named JSON options, and section-access policy at runtime via an
/// application-model convention — this assembly does not know which product hosts it.
/// </summary>
public static class ConversationsManagementApiConstants
{
    /// <summary>Conversation CRUD endpoints.</summary>
    public static class Conversations
    {
        /// <summary>Route segment for conversation endpoints.</summary>
        public const string RouteSegment = "conversations";

        /// <summary>OpenAPI group name for conversation endpoints.</summary>
        public const string GroupName = "Conversations";
    }

    /// <summary>Project CRUD endpoints.</summary>
    public static class Projects
    {
        /// <summary>Route segment for project endpoints.</summary>
        public const string RouteSegment = "projects";

        /// <summary>OpenAPI group name for project endpoints.</summary>
        public const string GroupName = "Projects";
    }
}
