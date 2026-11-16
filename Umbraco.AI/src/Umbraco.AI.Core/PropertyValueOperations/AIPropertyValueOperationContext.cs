using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Per-operation context passed to handlers. Carries the schema service, default-value provider,
/// document metadata, and a recursive dispatch callback so handlers can defer to other handlers
/// when constructing nested complex children.
/// </summary>
/// <remarks>
/// This class has no workspace dependencies and no state beyond the request being processed,
/// so it can be constructed identically by the HTTP endpoint (frontend tool path) and by future
/// in-process backend tool wrappers.
/// </remarks>
public sealed class AIPropertyValueOperationContext
{
    /// <summary>
    /// Initializes a new <see cref="AIPropertyValueOperationContext"/>.
    /// </summary>
    /// <param name="schemaService">The CMS property editor schema service.</param>
    /// <param name="defaultValueProvider">The default-value provider.</param>
    /// <param name="documentMetadata">The document-level metadata for the operation.</param>
    /// <param name="dispatcher">The dispatcher for recursive sub-operations.</param>
    public AIPropertyValueOperationContext(
        IPropertyEditorSchemaService schemaService,
        IAIPropertyDefaultValueProvider defaultValueProvider,
        AIDocumentMetadata documentMetadata,
        IAIPropertyValueDispatcher dispatcher)
    {
        SchemaService = schemaService;
        DefaultValueProvider = defaultValueProvider;
        DocumentMetadata = documentMetadata;
        Dispatcher = dispatcher;
    }

    /// <summary>Gets the CMS property editor schema service.</summary>
    public IPropertyEditorSchemaService SchemaService { get; }

    /// <summary>Gets the default-value provider for new items.</summary>
    public IAIPropertyDefaultValueProvider DefaultValueProvider { get; }

    /// <summary>Gets document-level metadata supplied by the caller.</summary>
    public AIDocumentMetadata DocumentMetadata { get; }

    /// <summary>
    /// Gets the culture/segment being edited. Falls back to the first entry of
    /// <see cref="AIDocumentMetadata.Variants"/> when not set explicitly.
    /// </summary>
    public AIVariantId? Variant
    {
        get => _variant ?? (DocumentMetadata.Variants.Count > 0 ? DocumentMetadata.Variants[0] : null);
        init => _variant = value;
    }

    private readonly AIVariantId? _variant;

    /// <summary>Gets the dispatcher for handlers that need to recurse into nested operations.</summary>
    public IAIPropertyValueDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the data type of the property the leaf operation runs against, when known. Handlers
    /// read editor configuration from it, such as which settings element type a block uses.
    /// </summary>
    /// <remarks>
    /// Set by the dispatcher before the leaf operation runs. <c>null</c> during the descent through
    /// parent blocks, and when the data type couldn't be loaded.
    /// </remarks>
    public IDataType? PropertyDataType { get; init; }

    /// <summary>Returns a copy of this context for an operation on a property with the given data type.</summary>
    internal AIPropertyValueOperationContext WithPropertyDataType(IDataType? dataType)
        => new(SchemaService, DefaultValueProvider, DocumentMetadata, Dispatcher)
        {
            Variant = _variant,
            PropertyDataType = dataType,
        };
}
