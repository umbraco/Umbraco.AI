using System.ComponentModel.DataAnnotations;

using Umbraco.Abilities.Samples.Automate.Plugins;

namespace Umbraco.Abilities.Samples.Commerce;

// Commerce's Automate package: a trigger and an action, written as Umbraco.Commerce.Automate writes them, with no
// Abilities code. Both need the Commerce section, so users without it are never offered them.

public sealed class StoreTriggerSettings
{
    // Commerce's store picker. No table maps it, so it stays a plain value whose description names the editor.
    [Field(Label = "Store", Description = "Only fire for this store. Leave blank to match all stores.", EditorUiAlias = "Uc.PropertyEditorUi.StorePicker")]
    public string? StoreId { get; set; }
}

[Trigger("umbracoCommerce.orderCreated", "Order Created", Description = "Fires when a new order is created.", RequiredSections = ["commerce"])]
public sealed class OrderCreatedTrigger : TriggerBase<StoreTriggerSettings>;

public sealed class ChangeOrderStatusSettings
{
    [Field(Label = "Order ID", Description = "The order to update.", SupportsBindings = true)]
    [Required]
    public string OrderId { get; set; } = string.Empty;

    [Field(Label = "Status ID", Description = "The new order status GUID.", SortOrder = 1, SupportsBindings = true)]
    [Required]
    public string OrderStatusId { get; set; } = string.Empty;
}

[Action("umbracoCommerce.changeOrderStatus", "Change Order Status", Description = "Changes the status of a Commerce order.", RequiredSections = ["commerce"])]
public sealed class ChangeOrderStatusAction : ActionBase<ChangeOrderStatusSettings>;
