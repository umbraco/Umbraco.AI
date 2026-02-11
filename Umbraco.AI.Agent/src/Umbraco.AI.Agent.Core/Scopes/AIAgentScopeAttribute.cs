namespace Umbraco.AI.Agent.Core.Scopes;

/// <summary>
/// Attribute to mark AI agent scope implementations for auto-discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="IAIAgentScope"/>
/// to enable automatic discovery and registration.
/// </para>
/// <para>
/// Name and description are resolved via localization on the frontend using:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentScope_{id}Label</c></item>
///   <item>Description: <c>uaiAgentScope_{id}Description</c></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentScope("copilot", Icon = "icon-chat")]
/// public class CopilotScope : AIAgentScopeBase { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIAgentScopeAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the icon to display for this scope.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-chat", "icon-document").
    /// Defaults to "icon-tag".
    /// </remarks>
    public string Icon { get; set; } = "icon-tag";

    /// <summary>
    /// Gets or sets the context dimensions this scope uses for filtering agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specify which aspects of the current context this scope considers when
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
    /// Initializes a new instance of the <see cref="AIAgentScopeAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the scope.</param>
    public AIAgentScopeAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }
}
