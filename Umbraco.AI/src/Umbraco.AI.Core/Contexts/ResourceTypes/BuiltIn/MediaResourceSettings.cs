using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Settings model for the live Media resource type. The media file is chosen with the standard media
/// picker (<c>Umbraco.MediaPicker3</c>), whose value is a list of picked entries — we ground the AI
/// with the first one.
/// </summary>
public sealed class MediaResourceSettings
{
    /// <summary>
    /// The picked media (limited to one). The standard media picker stores a list of entries; the AI
    /// is grounded with the first entry's media item, fetched at resolve time — subject to the acting
    /// user's read permission on the item.
    /// </summary>
    [AIField(
        EditorUiAlias = "Umb.PropertyEditorUi.MediaPicker",
        EditorConfig = "[{ \"alias\": \"validationLimit\", \"value\": { \"max\": 1 } }]",
        SortOrder = 10)]
    public IList<MediaResourcePickedItem>? Media { get; set; }
}

/// <summary>
/// A single entry from the media picker's value. Only the media reference is needed here; crops and
/// focal point (also stored by the picker) are irrelevant to grounding.
/// </summary>
public sealed class MediaResourcePickedItem
{
    /// <summary>The key (GUID) of the picked media item.</summary>
    public Guid? MediaKey { get; set; }
}
