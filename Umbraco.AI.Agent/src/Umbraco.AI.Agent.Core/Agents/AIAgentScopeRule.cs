namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Defines a single scope rule for agent availability.
/// All non-null/non-empty properties use AND logic between them.
/// Values within each array use OR logic.
/// </summary>
/// <remarks>
/// This follows the same pattern as Prompt scope rules.
/// </remarks>
/// <example>
/// Content-only rule:
/// <code>
/// new AIAgentScopeRule
/// {
///     Sections = ["content"]
/// }
/// </code>
/// Document editing rule (section AND entity type):
/// <code>
/// new AIAgentScopeRule
/// {
///     Sections = ["content"],
///     EntityTypes = ["document", "documentType"]
/// }
/// </code>
/// </example>
public sealed class AIAgentScopeRule
{
    /// <summary>
    /// Section pathnames where this rule applies (e.g., "content", "media").
    /// If any value matches the current section, this constraint is satisfied.
    /// Null or empty means any section.
    /// </summary>
    /// <example>["content", "media"]</example>
    public IReadOnlyList<string>? Sections { get; set; }

    /// <summary>
    /// Entity types where this rule applies (e.g., "document", "media").
    /// If any value matches the current entity type, this constraint is satisfied.
    /// Null or empty means any entity type.
    /// </summary>
    /// <example>["document", "documentType"]</example>
    public IReadOnlyList<string>? EntityTypes { get; set; }
}
