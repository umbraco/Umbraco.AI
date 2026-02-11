namespace Umbraco.AI.Agent.Core.Scopes;

/// <summary>
/// Interface for agent scope definitions.
/// </summary>
/// <remarks>
/// <para>
/// Scopes allow add-on packages to categorize agents for specific purposes.
/// For example, a "Copilot" scope might filter agents that should appear in a chat UI.
/// </para>
/// <para>
/// Scopes are discovered via the <see cref="AIAgentScopeAttribute"/> and registered
/// in the <see cref="AIAgentScopeCollection"/>.
/// </para>
/// <para>
/// Localization is handled by the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentScope_{scopeId}Label</c></item>
///   <item>Description: <c>uaiAgentScope_{scopeId}Description</c></item>
/// </list>
/// </para>
/// </remarks>
public interface IAIAgentScope
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    /// <remarks>
    /// This should be a simple, URL-safe string like "copilot" or "content-editing".
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the icon to display for this scope.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-chat", "icon-document").
    /// </remarks>
    string Icon { get; }

    /// <summary>
    /// Gets the context dimensions this scope uses for filtering agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Context dimensions define which aspects of the current context this scope
    /// considers when determining agent availability. Common dimensions include:
    /// <list type="bullet">
    ///   <item><c>"section"</c> - The current section (e.g., "content", "media")</item>
    ///   <item><c>"entityType"</c> - The current entity type (e.g., "document", "media")</item>
    ///   <item><c>"workspace"</c> - The current workspace (e.g., "Umb.Workspace.Document")</item>
    /// </list>
    /// </para>
    /// <para>
    /// For example, a Copilot scope might check ["section", "entityType"] to show
    /// different agents based on what section and entity type the user is viewing,
    /// while an API scope might only check ["entityType"] since APIs don't have sections.
    /// </para>
    /// <para>
    /// Empty list means the scope doesn't perform context-based filtering (all agents
    /// with this scope are available everywhere).
    /// </para>
    /// </remarks>
    /// <example>["section", "entityType"]</example>
    IReadOnlyList<string> SupportedContextDimensions { get; }
}
