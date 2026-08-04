using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Settings model for the live Content resource type.
/// </summary>
public sealed class ContentResourceSettings
{
    /// <summary>
    /// The key (GUID) of the content node to ground the AI with. Its current values are fetched at
    /// resolve time and injected as context — subject to the acting user's read permission on the node.
    /// </summary>
    [AIField(EditorUiAlias = "Umb.PropertyEditorUi.DocumentPicker", SortOrder = 10)]
    public Guid? ContentId { get; set; }
}
