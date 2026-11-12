using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.MediaPicker.Single</c> editor.
/// </summary>
/// <remarks>
/// The single media picker stores the same JSON array as <c>Umbraco.MediaPicker3</c>, so it shares
/// that handler's behaviour. The editor holds a single item, so adding a second is rejected.
/// </remarks>
public sealed class SingleMediaPickerPropertyValueHandler : MediaPicker3PropertyValueHandler
{
    private protected override string EditorAlias => CmsConstants.PropertyEditors.Aliases.SingleMediaPicker;

    private protected override int? MaxItems => 1;
}
