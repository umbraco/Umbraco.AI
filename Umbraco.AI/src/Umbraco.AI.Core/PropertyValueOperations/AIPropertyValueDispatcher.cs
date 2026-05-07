using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Default <see cref="IAIPropertyValueDispatcher"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Walks an <see cref="AIPropertyValueDispatchRequest.Path"/>, descends through nested values via
/// each level's handler, applies the requested operation at the leaf, and ascends rebuilding each
/// frame. Does not read or write data — the caller supplies the root value, the caller persists
/// the new root value.
/// </para>
/// <para>
/// CMS dependencies are limited to <see cref="IPropertyEditorSchemaService"/> (schema lookups for
/// validation), <see cref="IContentTypeService"/> (resolving editor schema aliases of properties
/// nested inside blocks), and the default-value provider abstraction.
/// </para>
/// </remarks>
public sealed class AIPropertyValueDispatcher : IAIPropertyValueDispatcher
{
    private static readonly JsonSerializerOptions ArgsSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AIPropertyValueHandlerCollection _handlers;
    private readonly IPropertyEditorSchemaService _schemaService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IAIPropertyDefaultValueProvider _defaultValueProvider;
    private readonly ILogger<AIPropertyValueDispatcher> _logger;

    /// <summary>
    /// Initializes a new <see cref="AIPropertyValueDispatcher"/>.
    /// </summary>
    public AIPropertyValueDispatcher(
        AIPropertyValueHandlerCollection handlers,
        IPropertyEditorSchemaService schemaService,
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IAIPropertyDefaultValueProvider defaultValueProvider,
        ILogger<AIPropertyValueDispatcher> logger)
    {
        _handlers = handlers;
        _schemaService = schemaService;
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _defaultValueProvider = defaultValueProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AIPropertyValueDispatchResult> DispatchAsync(
        AIPropertyValueDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Path is null || request.Path.Count == 0)
        {
            return AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.InvalidPath,
                "Path must contain at least one segment."));
        }

        if (string.IsNullOrWhiteSpace(request.RootEditorSchemaAlias))
        {
            return AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.InvalidRootValue,
                "RootEditorSchemaAlias must be supplied."));
        }

        // The first segment is always a property alias identifying the root property. We do not
        // descend through it — the root value is already supplied directly.
        if (request.Path[0] is not AIPropertyPathSegment.PropertyAliasSegment)
        {
            return AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.InvalidPath,
                "Path must begin with a property alias segment."));
        }

        try
        {
            var context = new AIPropertyValueOperationContext(
                _schemaService,
                _defaultValueProvider,
                request.DocumentMetadata,
                this);

            return await DispatchInternalAsync(request, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Property value dispatch failed for editor '{Editor}' op '{Op}'.",
                request.RootEditorSchemaAlias, request.Operation);

            return AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.Internal,
                $"Internal error: {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private async Task<AIPropertyValueDispatchResult> DispatchInternalAsync(
        AIPropertyValueDispatchRequest request,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken)
    {
        // Build the descent stack. Each frame represents a level we walked into via a
        // block-key selector; on the way back up we ascend by reversing them.
        var path = request.Path;
        var frames = new List<DescentFrame>();

        var currentEditorSchemaAlias = request.RootEditorSchemaAlias;
        var currentValue = request.RootValue;

        // Path layout: [propAlias, {blockKey}, propAlias, {blockKey}, ..., propAlias]
        // - Even indices: property alias segments (root, then each property nested in the previous block)
        // - Odd indices: block key selectors

        // Walk each (propertyAlias, blockKey) pair. The last propertyAlias is the leaf.
        for (var i = 0; i < path.Count; i++)
        {
            var segment = path[i];

            if ((i & 1) == 0)
            {
                if (segment is not AIPropertyPathSegment.PropertyAliasSegment)
                {
                    return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                        $"Expected property alias at path index {i}.");
                }
            }
            else
            {
                if (segment is not AIPropertyPathSegment.BlockKeySegment)
                {
                    return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                        $"Expected block selector at path index {i}.");
                }
            }
        }

        // Walk: each iteration consumes a (propAlias, blockSelector) pair and descends one level.
        var leafPropertyAliasIndex = path.Count - 1;
        if ((leafPropertyAliasIndex & 1) != 0)
        {
            // Last segment is a block selector, which is invalid: paths must end on a property alias.
            return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                "Path must end with a property alias segment.");
        }

        for (var i = 0; i + 1 < path.Count; i += 2)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var propertyAlias = ((AIPropertyPathSegment.PropertyAliasSegment)path[i]).Alias;
            var blockKey = ((AIPropertyPathSegment.BlockKeySegment)path[i + 1]).BlockKey;

            // Resolve handler for the current frame (the editor we're descending out of).
            var handler = _handlers.GetByEditorSchemaAlias(currentEditorSchemaAlias);
            if (handler is null)
            {
                return Fail(AIPropertyValueOperationError.Codes.NoHandler,
                    $"No property value handler is registered for editor '{currentEditorSchemaAlias}'.");
            }

            // Snapshot the frame so we can ascend later.
            frames.Add(new DescentFrame(handler, currentValue, blockKey, propertyAlias));

            // Descend: find the block's content type, then read the inner property's value.
            var innerContentTypeKey = handler.GetItemContentTypeKey(currentValue, blockKey, context);
            if (innerContentTypeKey is null)
            {
                return Fail(AIPropertyValueOperationError.Codes.BlockNotFound,
                    $"Block '{blockKey}' was not found inside property '{propertyAlias}', or this editor does not support nested items.");
            }

            // Look up the next-level property's editor schema alias from its content type.
            var nextPropertyAlias = ((AIPropertyPathSegment.PropertyAliasSegment)path[i + 2]).Alias;

            var nextEditorSchemaAlias = TryResolvePropertyEditorAlias(innerContentTypeKey.Value, nextPropertyAlias);
            if (nextEditorSchemaAlias is null)
            {
                return Fail(AIPropertyValueOperationError.Codes.PropertyNotFound,
                    $"Property '{nextPropertyAlias}' was not found on content type '{innerContentTypeKey}'.");
            }

            // Read the next-level value.
            currentValue = handler.GetItemPropertyValue(
                currentValue, blockKey, nextPropertyAlias, variantId: null, context);

            currentEditorSchemaAlias = nextEditorSchemaAlias;
        }

        // currentValue is the leaf value; currentEditorSchemaAlias is the leaf's editor.
        // Apply the leaf operation.
        var leafResult = await ApplyLeafOperationAsync(
            request, currentEditorSchemaAlias, currentValue, context, cancellationToken)
            .ConfigureAwait(false);

        if (!leafResult.Success)
        {
            return leafResult;
        }

        // Ascend: rebuild each frame's value with the mutated child.
        var ascendingValue = leafResult.NewRootValue;
        var leafPropertyAlias = ((AIPropertyPathSegment.PropertyAliasSegment)path[leafPropertyAliasIndex]).Alias;
        var ascendingPropertyAlias = leafPropertyAlias;

        for (var i = frames.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var frame = frames[i];

            ascendingValue = await frame.Handler.SetItemPropertyValueAsync(
                frame.Value,
                frame.BlockKey,
                ascendingPropertyAlias,
                ascendingValue,
                variantId: null,
                context,
                cancellationToken).ConfigureAwait(false);

            ascendingPropertyAlias = frame.PropertyAlias;
        }

        return AIPropertyValueDispatchResult.Ok(ascendingValue, leafResult.BlockKey);
    }

    private async Task<AIPropertyValueDispatchResult> ApplyLeafOperationAsync(
        AIPropertyValueDispatchRequest request,
        string leafEditorSchemaAlias,
        JsonNode? leafValue,
        AIPropertyValueOperationContext context,
        CancellationToken cancellationToken)
    {
        // SetValue and ClearValue do not need a handler — they replace or empty the leaf scalar.
        switch (request.Operation)
        {
            case AIPropertyOperation.SetValue:
                {
                    var newLeaf = ExtractValueArg(request.Args);
                    return AIPropertyValueDispatchResult.Ok(newLeaf?.DeepClone());
                }

            case AIPropertyOperation.ClearValue:
                {
                    // For collection editors, defer to the handler's Clear so the editor's
                    // canonical empty representation is used; for scalars, null suffices.
                    var clearHandler = _handlers.GetByEditorSchemaAlias(leafEditorSchemaAlias);
                    if (clearHandler is not null)
                    {
                        var cleared = await clearHandler.ClearAsync(leafValue, context, cancellationToken).ConfigureAwait(false);
                        return AIPropertyValueDispatchResult.Ok(cleared);
                    }

                    return AIPropertyValueDispatchResult.Ok(null);
                }
        }

        // AddItem / RemoveItem / MoveItem require a handler.
        var handler = _handlers.GetByEditorSchemaAlias(leafEditorSchemaAlias);
        if (handler is null)
        {
            return Fail(AIPropertyValueOperationError.Codes.NoHandler,
                $"No property value handler is registered for editor '{leafEditorSchemaAlias}'. Use set_value with a complete value.");
        }

        switch (request.Operation)
        {
            case AIPropertyOperation.AddItem:
                {
                    var args = DeserializeArgs<AIAddItemArgs>(request.Args) ?? new AIAddItemArgs();

                    var validation = handler.ValidateAddItem(leafValue, args, context);
                    if (!validation.IsValid)
                    {
                        return AIPropertyValueDispatchResult.Fail(validation.Error!);
                    }

                    var addResult = await handler.AddItemAsync(leafValue, args, context, cancellationToken).ConfigureAwait(false);
                    return AIPropertyValueDispatchResult.Ok(addResult.Value, addResult.BlockKey);
                }

            case AIPropertyOperation.RemoveItem:
                {
                    if (!TryReadGuidArg(request.Args, "blockKey", out var blockKey))
                    {
                        return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                            "RemoveItem requires a 'blockKey' GUID argument.");
                    }

                    var newValue = await handler.RemoveItemAsync(leafValue, blockKey, context, cancellationToken).ConfigureAwait(false);
                    return AIPropertyValueDispatchResult.Ok(newValue);
                }

            case AIPropertyOperation.MoveItem:
                {
                    if (!TryReadGuidArg(request.Args, "blockKey", out var blockKey))
                    {
                        return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                            "MoveItem requires a 'blockKey' GUID argument.");
                    }

                    if (!TryReadIntArg(request.Args, "position", out var position))
                    {
                        return Fail(AIPropertyValueOperationError.Codes.InvalidPath,
                            "MoveItem requires an integer 'position' argument.");
                    }

                    var newValue = await handler.MoveItemAsync(leafValue, blockKey, position, context, cancellationToken).ConfigureAwait(false);
                    return AIPropertyValueDispatchResult.Ok(newValue);
                }

            default:
                return Fail(AIPropertyValueOperationError.Codes.OperationNotSupported,
                    $"Operation '{request.Operation}' is not supported.");
        }
    }

    private string? TryResolvePropertyEditorAlias(Guid contentTypeKey, string propertyAlias)
    {
        // Try content types (covers documents and elements) first, then media types.
        var composition = (IContentTypeComposition?)_contentTypeService.Get(contentTypeKey)
            ?? _mediaTypeService.Get(contentTypeKey);

        if (composition is null)
        {
            return null;
        }

        var property = composition.CompositionPropertyTypes
            .FirstOrDefault(p => string.Equals(p.Alias, propertyAlias, StringComparison.OrdinalIgnoreCase));

        return property?.PropertyEditorAlias;
    }

    private static JsonNode? ExtractValueArg(JsonNode? args)
    {
        if (args is JsonObject obj && obj.TryGetPropertyValue("value", out var valueNode))
        {
            return valueNode;
        }

        return null;
    }

    private static T? DeserializeArgs<T>(JsonNode? args)
    {
        if (args is null)
        {
            return default;
        }

        return args.Deserialize<T>(ArgsSerializerOptions);
    }

    private static bool TryReadGuidArg(JsonNode? args, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (args is not JsonObject obj || !obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return false;
        }

        var raw = node.GetValue<string?>();
        return Guid.TryParse(raw, out value);
    }

    private static bool TryReadIntArg(JsonNode? args, string propertyName, out int value)
    {
        value = 0;
        if (args is not JsonObject obj || !obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return false;
        }

        try
        {
            value = node.GetValue<int>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AIPropertyValueDispatchResult Fail(string code, string message)
        => AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(code, message));

    private readonly record struct DescentFrame(
        IAIPropertyValueHandler Handler,
        JsonNode? Value,
        Guid BlockKey,
        string PropertyAlias);
}
