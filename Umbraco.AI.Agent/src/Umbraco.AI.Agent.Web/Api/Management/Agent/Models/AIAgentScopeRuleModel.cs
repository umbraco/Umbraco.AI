namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for a single scope rule.
/// All non-null/non-empty properties use AND logic between them.
/// Values within each array use OR logic.
/// </summary>
public class AIAgentScopeRuleModel
{
    /// <summary>
    /// Section pathnames where this rule applies (e.g., "content", "media").
    /// If any value matches the current section, this constraint is satisfied.
    /// Null or empty means any section.
    /// </summary>
    public IReadOnlyList<string>? SectionAliases { get; set; }

    /// <summary>
    /// Entity type aliases where this rule applies (e.g., "document", "media").
    /// If any value matches the current entity type, this constraint is satisfied.
    /// Null or empty means any entity type.
    /// </summary>
    public IReadOnlyList<string>? EntityTypeAliases { get; set; }

    /// <summary>
    /// Workspace aliases where this rule applies (e.g., "Umb.Workspace.Document").
    /// If any value matches the current workspace, this constraint is satisfied.
    /// Null or empty means any workspace.
    /// </summary>
    public IReadOnlyList<string>? WorkspaceAliases { get; set; }
}
