namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Attribute to mark tool scope implementations for auto-discovery.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="IAIToolScope"/>
/// to enable automatic discovery and registration.
/// </para>
/// <para>
/// Name and description are resolved via localization on the frontend using:
/// <list type="bullet">
///   <item>Name: <c>uaiToolScope_{id}Label</c></item>
///   <item>Description: <c>uaiToolScope_{id}Description</c></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIToolScope("content-read", Icon = "icon-folder", Domain = "Content")]
/// public class ContentReadScope : AIToolScopeBase { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIToolScopeAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the icon to display for this scope.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-folder", "icon-edit").
    /// Defaults to "icon-tag".
    /// </remarks>
    public string Icon { get; set; } = "icon-tag";

    /// <summary>
    /// Gets or sets whether tools in this scope are destructive.
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Gets or sets the domain grouping for UI organization.
    /// </summary>
    /// <remarks>
    /// Defaults to "General".
    /// </remarks>
    public string Domain { get; set; } = "General";

    /// <summary>
    /// Initializes a new instance of the <see cref="AIToolScopeAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the scope.</param>
    public AIToolScopeAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }
}
