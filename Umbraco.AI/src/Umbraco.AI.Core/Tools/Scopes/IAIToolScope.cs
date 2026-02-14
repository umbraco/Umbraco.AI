namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Interface for tool scope definitions.
/// </summary>
/// <remarks>
/// <para>
/// Tool scopes categorize tools by their operational scope (e.g., content-read, content-write).
/// They enable bulk tool enablement via scopes and clear destructiveness marking.
/// </para>
/// <para>
/// Scopes are discovered via the <see cref="AIToolScopeAttribute"/> and registered
/// in the <see cref="AIToolScopeCollection"/>.
/// </para>
/// <para>
/// Localization is handled by the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiToolScope_{scopeId}Label</c></item>
///   <item>Description: <c>uaiToolScope_{scopeId}Description</c></item>
/// </list>
/// </para>
/// </remarks>
public interface IAIToolScope
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    /// <remarks>
    /// Examples: "content-read", "content-write", "media-read", "search"
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the icon to display for this scope.
    /// </summary>
    /// <remarks>
    /// Uses Umbraco icon names (e.g., "icon-folder", "icon-edit").
    /// </remarks>
    string Icon { get; }

    /// <summary>
    /// Gets whether tools in this scope are destructive (modify data).
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets the domain grouping for UI organization.
    /// </summary>
    /// <remarks>
    /// Used to group related scopes in the UI (e.g., "Content", "Media", "General").
    /// </remarks>
    string Domain { get; }

    /// <summary>
    /// Gets the entity types this scope is relevant for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specifies which entity types this scope's tools are designed to work with.
    /// Used for runtime context filtering - tools are only included when the current
    /// context matches one of the declared entity types.
    /// </para>
    /// <para>
    /// Empty list means the scope is available for all entity types (e.g., "search", "navigation").
    /// Non-empty list restricts the scope to specific entity types (e.g., ["document", "media"]).
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item>Content scopes: ["document", "documentType"]</item>
    ///   <item>Media scopes: ["media", "mediaType"]</item>
    ///   <item>General scopes: [] (available everywhere)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>["document", "media"]</example>
    IReadOnlyList<string> ForEntityTypes { get; }
}
