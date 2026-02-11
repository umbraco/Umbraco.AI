namespace Umbraco.AI.Agent.Core.Surfaces;

/// <summary>
/// Attribute to mark AI agent surface implementations for auto-discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="IAIAgentSurface"/>
/// to enable automatic discovery and registration.
/// </para>
/// <para>
/// Name and description are resolved via localization on the frontend using:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentSurface_{id}Label</c></item>
///   <item>Description: <c>uaiAgentSurface_{id}Description</c></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentSurface("copilot", Icon = "icon-chat")]
/// public class CopilotSurface : AIAgentSurfaceBase { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIAgentSurfaceAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this surface.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the icon to display for this surface.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-chat", "icon-document").
    /// Defaults to "icon-tag".
    /// </remarks>
    public string Icon { get; set; } = "icon-tag";

    /// <summary>
    /// Gets or sets the context dimensions this surface uses for filtering agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specify which aspects of the current context this surface considers when
    /// determining agent availability. Common dimensions:
    /// <list type="bullet">
    ///   <item><c>"section"</c> - Current section (e.g., "content", "media")</item>
    ///   <item><c>"entityType"</c> - Current entity type (e.g., "document", "media")</item>
    ///   <item><c>"workspace"</c> - Current workspace (e.g., "Umb.Workspace.Document")</item>
    /// </list>
    /// </para>
    /// <para>
    /// Empty or null means no context-based filtering (all agents available everywhere).
    /// </para>
    /// </remarks>
    /// <example>new[] { "section", "entityType" }</example>
    public string[]? SupportedContextDimensions { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSurfaceAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the surface.</param>
    public AIAgentSurfaceAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }
}
