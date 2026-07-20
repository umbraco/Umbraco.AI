using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Settings model for the live CMS-content resource type.
/// </summary>
public sealed class CmsContentResourceSettings
{
    /// <summary>
    /// The key (GUID) of the content node to ground the AI with. Its current values are fetched at
    /// resolve time and injected as context — subject to the acting user's read permission on the node.
    /// </summary>
    /// <remarks>
    /// MVP uses a plain GUID input. TODO: swap to <c>Umb.PropertyEditorUi.DocumentPicker</c> (with proper
    /// picker-value parsing) and support subtree/media selection.
    /// </remarks>
    [AIField(EditorUiAlias = "Umb.PropertyEditorUi.TextBox", SortOrder = 10)]
    public string? ContentId { get; set; }
}
