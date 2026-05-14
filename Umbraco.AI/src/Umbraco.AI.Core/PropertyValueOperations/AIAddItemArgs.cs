using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Arguments passed to a property value handler when adding a new item to a collection.
/// </summary>
/// <param name="ElementType">
/// Optional element type alias OR GUID identifying the kind of item to add. Required for editors
/// that discriminate between element types (block-list, block-grid). Ignored for editors with a
/// single shape (most pickers).
/// </param>
/// <param name="Values">
/// Optional initial values for the item, keyed by property alias. Properties not supplied are
/// filled in from the editor's default-value provider.
/// </param>
/// <param name="SettingsValues">
/// Optional initial settings values for editors that support a settings element alongside the
/// content element (block-list / block-grid).
/// </param>
/// <param name="Position">
/// Optional zero-based insertion index. <c>null</c> appends to the end of the collection.
/// </param>
/// <param name="Variant">
/// Optional variant identifier scoping the operation to a specific culture/segment when the parent
/// is variant-aware. <c>null</c> defaults to the first variant in <see cref="AIDocumentMetadata.Variants"/>.
/// </param>
/// <param name="Extra">
/// Editor-specific additional arguments (e.g. block-grid <c>gridArea</c>). Handlers may inspect this
/// object for fields they understand and reject unknown values via <see cref="AIValidationResult"/>.
/// </param>
public sealed record AIAddItemArgs(
    string? ElementType = null,
    JsonObject? Values = null,
    JsonObject? SettingsValues = null,
    int? Position = null,
    AIVariantId? Variant = null,
    JsonObject? Extra = null);
