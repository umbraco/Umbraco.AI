namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// API model for a visibility rule that determines where a prompt can appear.
/// </summary>
public class VisibilityRuleModel
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
    /// Document type aliases to match (e.g., 'article', 'blogPost').
    /// If any value matches the current document type, this constraint is satisfied.
    /// Null or empty means any document type.
    /// </summary>
    public IEnumerable<string>? DocumentTypeAliases { get; set; }
}
