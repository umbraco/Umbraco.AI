using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the AddUmbracoContentItem tool.
/// </summary>
public record AddUmbracoContentItemArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Path identifying the collection-shaped property (Block List, Block Grid, etc.) to add the item to, potentially nested inside another block. Must start and end with an alias segment.")]
    IReadOnlyList<UmbracoPropertyPathSegmentArg> Path,

    [property: Description("The element type alias or key identifying the kind of item to add (required for Block List/Block Grid, which can allow multiple element types — see x-allowedElementTypes from get_content_type_schema). Ignored for editors with a single shape.")]
    string? ElementType,

    [property: Description("Initial values for the new item's own properties, keyed by property alias. Call get_content_type_schema with the element type to see valid aliases and value shapes. Properties not supplied are left empty (there is currently no automatic default-value filling).")]
    Dictionary<string, JsonElement>? Values,

    [property: Description("Initial values for the new item's settings element (Block List/Block Grid only), keyed by property alias.")]
    Dictionary<string, JsonElement>? SettingsValues,

    [property: Description("Optional zero-based insertion position. Omit to append to the end of the collection.")]
    int? Position,

    [property: Description("Optional culture code (e.g., 'en-US') when the content item varies by culture.")]
    string? Culture = null,

    [property: Description("Optional segment identifier when the content item is segmented.")]
    string? Segment = null);

/// <summary>
/// Tool that adds a new item to a collection-shaped content property (Block List, Block Grid, etc.),
/// including nested inside another block. Returns the new item's key, which can be used as a
/// BlockKey path segment in a follow-up call to populate a nested property inside it, or to
/// remove_umbraco_content_item / move_umbraco_content_item it later.
/// </summary>
[AITool("add_umbraco_content_item", "Add Umbraco Content Item", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class AddUmbracoContentItemTool(
    IContentEditingService contentEditingService,
    IAIPropertyValueDispatcher dispatcher,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<AddUmbracoContentItemArgs>
{
    private static readonly JsonSerializerOptions AddItemArgsSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public override string Description =>
        "Adds a new item to a collection-shaped content property (Block List, Block Grid, etc.), " +
        "including nested inside another block — for a Block List inside a block, chain calls: first " +
        "add_umbraco_content_item on the outer property, then a second call whose Path descends into the " +
        "returned BlockKey. NOTE: rich text properties reject this operation — embed a block into rich " +
        "text by set_umbraco_content_value-ing a markup placeholder first, then targeting the embedded " +
        "block's properties via Path. Call get_content_type_schema first to discover valid element types " +
        "and property shapes. Persists immediately as a draft — call publish_umbraco_content afterward to " +
        "make the change live.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(AddUmbracoContentItemArgs args, CancellationToken cancellationToken = default)
    {
        var addItemArgs = new AIAddItemArgs(
            ElementType: args.ElementType,
            Values: ToJsonObject(args.Values),
            SettingsValues: ToJsonObject(args.SettingsValues),
            Position: args.Position);
        var dispatchArgs = JsonSerializer.SerializeToNode(addItemArgs, AddItemArgsSerializerOptions);

        var outcome = await ContentPropertyValueOperationHelper.ExecuteAsync(
            authorizer,
            contentEditingService,
            dispatcher,
            args.Key,
            args.Path,
            AIPropertyOperation.AddItem,
            dispatchArgs,
            args.Culture,
            args.Segment,
            cancellationToken);

        return new AddUmbracoContentItemResult(outcome.Success, outcome.BlockKey, outcome.Message);
    }

    private static JsonObject? ToJsonObject(Dictionary<string, JsonElement>? values)
    {
        if (values is null)
        {
            return null;
        }

        var obj = new JsonObject();
        foreach (var (key, value) in values)
        {
            obj[key] = JsonNode.Parse(value.GetRawText());
        }

        return obj;
    }
}

/// <summary>
/// Result of the add Umbraco content item tool.
/// </summary>
public record AddUmbracoContentItemResult(
    bool Success,
    Guid? BlockKey,
    string? Message);
