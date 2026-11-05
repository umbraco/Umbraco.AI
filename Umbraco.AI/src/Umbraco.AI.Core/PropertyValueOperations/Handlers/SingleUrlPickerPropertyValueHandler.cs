using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Core.PropertyValueOperations.Handlers;

/// <summary>
/// Property value handler for the <c>Umbraco.UrlPicker.Single</c> editor.
/// </summary>
/// <remarks>
/// The single URL picker stores the same JSON array as <c>Umbraco.MultiUrlPicker</c>, so it shares
/// that handler's behaviour. The editor holds a single link, so adding a second is rejected.
/// </remarks>
public sealed class SingleUrlPickerPropertyValueHandler : MultiUrlPickerPropertyValueHandler
{
    private protected override string EditorAlias => CmsConstants.PropertyEditors.Aliases.SingleUrlPicker;

    private protected override int? MaxItems => 1;
}
