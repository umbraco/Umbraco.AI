namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Defines a single scope rule that determines where a prompt can run.
/// All non-null properties use AND logic between them.
/// Values within each array use OR logic.
/// </summary>
public class AiPromptScopeRule
{
    /// <summary>
    /// Property Editor UI aliases to match (e.g., 'Umb.PropertyEditorUi.TextBox').
    /// If any value matches the current property editor UI, this constraint is satisfied.
    /// Null or empty means any property editor UI.
    /// </summary>
    public IReadOnlyList<string>? PropertyEditorUiAliases { get; set; }

    /// <summary>
    /// Property aliases to match (e.g., 'pageTitle', 'description').
    /// If any value matches the current property alias, this constraint is satisfied.
    /// Null or empty means any property.
    /// </summary>
    public IReadOnlyList<string>? PropertyAliases { get; set; }

    /// <summary>
    /// Content type aliases to match (e.g., 'article', 'blogPost').
    /// If any value matches the current content type, this constraint is satisfied.
    /// Null or empty means any content type.
    /// When specified, property constraints only apply to these content types.
    /// </summary>
    public IReadOnlyList<string>? ContentTypeAliases { get; set; }
}
