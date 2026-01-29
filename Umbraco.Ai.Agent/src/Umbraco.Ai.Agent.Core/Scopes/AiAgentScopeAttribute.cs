namespace Umbraco.Ai.Agent.Core.Scopes;

/// <summary>
/// Attribute to mark AI agent scope implementations for auto-discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="IAiAgentScope"/>
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
/// [AiAgentScope("copilot", Icon = "icon-chat")]
/// public class CopilotScope : AiAgentScopeBase { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiAgentScopeAttribute : Attribute
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
    /// Initializes a new instance of the <see cref="AiAgentScopeAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the scope.</param>
    public AiAgentScopeAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }
}
