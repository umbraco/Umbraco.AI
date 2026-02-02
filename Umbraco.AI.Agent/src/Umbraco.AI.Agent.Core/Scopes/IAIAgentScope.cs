namespace Umbraco.Ai.Agent.Core.Scopes;

/// <summary>
/// Interface for agent scope definitions.
/// </summary>
/// <remarks>
/// <para>
/// Scopes allow add-on packages to categorize agents for specific purposes.
/// For example, a "Copilot" scope might filter agents that should appear in a chat UI.
/// </para>
/// <para>
/// Scopes are discovered via the <see cref="AiAgentScopeAttribute"/> and registered
/// in the <see cref="AiAgentScopeCollection"/>.
/// </para>
/// <para>
/// Localization is handled by the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentScope_{scopeId}Label</c></item>
///   <item>Description: <c>uaiAgentScope_{scopeId}Description</c></item>
/// </list>
/// </para>
/// </remarks>
public interface IAiAgentScope
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
}
