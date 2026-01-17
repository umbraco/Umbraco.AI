namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// API model for a scope rule that determines where a prompt can run.
/// </summary>
public class ScopeRuleModel
{
    /// <summary>
    /// Property Editor UI aliases to match (e.g., 'Umb.PropertyEditorUi.TextBox').
    /// If any value matches the current property editor UI, this constraint is satisfied.
    /// Null or empty means any property editor UI.
    /// </summary>
    public IEnumerable<string>? PropertyEditorUiAliases { get; set; }

    /// <summary>
    /// Property aliases to match (e.g., 'pageTitle', 'description').
    /// If any value matches the current property alias, this constraint is satisfied.
    /// Null or empty means any property.
    /// </summary>
    public IEnumerable<string>? PropertyAliases { get; set; }

    /// <summary>
    /// Content type aliases to match (e.g., 'article', 'blogPost').
    /// If any value matches the current content type, this constraint is satisfied.
    /// Null or empty means any content type.
    /// </summary>
    public IEnumerable<string>? ContentTypeAliases { get; set; }
}
