namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Defines a single context scope rule for agent availability.
/// All non-null/non-empty properties use AND logic between them.
/// Values within each array use OR logic.
/// </summary>
/// <remarks>
/// This follows the same pattern as Prompt scope rules.
/// </remarks>
/// <example>
/// Content-only rule:
/// <code>
/// new AIAgentContextScopeRule
/// {
///     SectionAliases = ["content"]
/// }
/// </code>
/// Document editing rule (section AND entity type):
/// <code>
/// new AIAgentContextScopeRule
/// {
///     SectionAliases = ["content"],
///     EntityTypeAliases = ["document", "documentType"]
/// }
/// </code>
/// </example>
public sealed class AIAgentContextScopeRule
{
    /// <summary>
    /// Section pathnames where this rule applies (e.g., "content", "media").
    /// If any value matches the current section, this constraint is satisfied.
    /// Null or empty means any section.
    /// </summary>
    /// <example>["content", "media"]</example>
    public IReadOnlyList<string>? SectionAliases { get; set; }

    /// <summary>
    /// Entity type aliases where this rule applies (e.g., "document", "media").
    /// If any value matches the current entity type, this constraint is satisfied.
    /// Null or empty means any entity type.
    /// </summary>
    /// <example>["document", "documentType"]</example>
    public IReadOnlyList<string>? EntityTypeAliases { get; set; }

    /// <summary>
    /// Workspace aliases where this rule applies (e.g., "Umb.Workspace.Document").
    /// If any value matches the current workspace, this constraint is satisfied.
    /// Null or empty means any workspace.
    /// </summary>
    /// <example>["Umb.Workspace.Document"]</example>
    public IReadOnlyList<string>? WorkspaceAliases { get; set; }
}
