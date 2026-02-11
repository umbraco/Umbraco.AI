namespace Umbraco.AI.Agent.Core.Surfaces;

/// <summary>
/// Interface for agent surface definitions.
/// </summary>
/// <remarks>
/// <para>
/// Surfaces allow add-on packages to categorize agents for specific purposes.
/// For example, a "Copilot" surface might filter agents that should appear in a chat UI.
/// </para>
/// <para>
/// Surfaces are discovered via the <see cref="AIAgentSurfaceAttribute"/> and registered
/// in the <see cref="AIAgentSurfaceCollection"/>.
/// </para>
/// <para>
/// Localization is handled by the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentSurface_{surfaceId}Label</c></item>
///   <item>Description: <c>uaiAgentSurface_{surfaceId}Description</c></item>
/// </list>
/// </para>
/// </remarks>
public interface IAIAgentSurface
{
    /// <summary>
    /// Gets the unique identifier for this surface.
    /// </summary>
    /// <remarks>
    /// This should be a simple, URL-safe string like "copilot" or "content-editing".
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the icon to display for this surface.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-chat", "icon-document").
    /// </remarks>
    string Icon { get; }

    /// <summary>
    /// Gets the context dimensions this surface uses for filtering agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Context dimensions define which aspects of the current context this surface
    /// considers when determining agent availability. Common dimensions include:
    /// <list type="bullet">
    ///   <item><c>"section"</c> - The current section (e.g., "content", "media")</item>
    ///   <item><c>"entityType"</c> - The current entity type (e.g., "document", "media")</item>
    ///   <item><c>"workspace"</c> - The current workspace (e.g., "Umb.Workspace.Document")</item>
    /// </list>
    /// </para>
    /// <para>
    /// For example, a Copilot surface might check ["section", "entityType"] to show
    /// different agents based on what section and entity type the user is viewing,
    /// while an API surface might only check ["entityType"] since APIs don't have sections.
    /// </para>
    /// <para>
    /// Empty list means the surface doesn't perform context-based filtering (all agents
    /// with this surface are available everywhere).
    /// </para>
    /// </remarks>
    /// <example>["section", "entityType"]</example>
    IReadOnlyList<string> SupportedContextDimensions { get; }
}
