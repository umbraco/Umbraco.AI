using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.Abilities.Cms;

internal enum PropertyKind
{
    Field,
    BlockList,
    BlockGrid,
}

/// <summary>One configured block: its content and settings element types, and (Block Grid) its areas.</summary>
internal sealed record BlockMap(Guid ContentElementTypeKey, string ContentAlias, Guid? SettingsElementTypeKey, IReadOnlyList<AreaMap> Areas);

internal sealed record AreaMap(Guid Key, string Alias);

/// <summary>How one property maps: to a field, which for a block editor is an items field of blocks.</summary>
internal sealed record PropertyMap(string Alias, string EditorAlias, PropertyKind Kind, FieldDefinition Field, IReadOnlyList<BlockMap> Blocks)
{
    public BlockMap? BlockFor(string elementAlias) => Blocks.FirstOrDefault(b => b.ContentAlias == elementAlias);
}

/// <summary>The shape of a document or element type, plus how each of its properties maps.</summary>
internal sealed record ShapeMap(ElementShape Shape, ContentVariation Variations, IReadOnlyDictionary<string, PropertyMap> Properties);

/// <summary>
/// Builds the structure language from Umbraco document types, element types and data types. Block editors
/// become items fields whose item types are registered per data type, because the same element type can be used by
/// several block editors with different settings types and areas.
/// </summary>
internal sealed class CmsStructureMapper(
    IContentTypeService contentTypes,
    IDataTypeService dataTypes,
    ItemTypeRegistry itemTypes,
    IPropertyEditorSchemaService? schemas,
    IReadOnlyDictionary<string, IValueHandler> editorHandlers)
{
    public const string NameField = "name";
    public const string ParentField = "parent";

    private readonly HashSet<Guid> _dataTypesInProgress = [];

    public IContentType RequireType(Guid key)
        => contentTypes.Get(key) ?? throw new InvalidOperationException($"No content type with key {key}.");

    public async Task<ShapeMap> MapAsync(IContentTypeComposition type, bool isDocument)
    {
        var fields = new List<FieldDefinition>();
        var properties = new Dictionary<string, PropertyMap>(StringComparer.Ordinal);

        if (isDocument)
        {
            fields.Add(new FieldDefinition(NameField, "The document's name.", FieldType.String, Required: true)
            {
                Variations = type.VariesByCulture() ? ContentVariation.Culture : ContentVariation.Nothing,
            });
            fields.Add(new FieldDefinition(ParentField, "The parent document. Only used when creating; moving is a separate operation.", FieldType.EntityReference)
            {
                ReferencesEntityType = CmsDocumentDefinition.Type,
            });
        }

        foreach (var propertyType in type.CompositionPropertyTypes)
        {
            var map = await MapPropertyAsync(propertyType);
            properties[propertyType.Alias] = map;
            fields.Add(map.Field);
        }

        return new ShapeMap(new ElementShape(fields), type.Variations, properties);
    }

    private async Task<PropertyMap> MapPropertyAsync(IPropertyType propertyType)
    {
        var alias = propertyType.Alias;
        var editor = propertyType.PropertyEditorAlias;
        var description = string.IsNullOrWhiteSpace(propertyType.Description) ? propertyType.Name : propertyType.Description;
        var varies = propertyType.Variations;

        PropertyMap AsField(FieldType type, bool many = false, string? references = null)
            => new(alias, editor, PropertyKind.Field, new FieldDefinition(alias, description, type, propertyType.Mandatory)
            {
                Variations = varies,
                Many = many,
                ReferencesEntityType = references,
            }, []);

        switch (editor)
        {
            case Constants.PropertyEditors.Aliases.TextBox:
            case Constants.PropertyEditors.Aliases.TextArea:
                return AsField(FieldType.String);
            case Constants.PropertyEditors.Aliases.Integer:
                return AsField(FieldType.Integer);
            case Constants.PropertyEditors.Aliases.Decimal:
                return AsField(FieldType.Number);
            case Constants.PropertyEditors.Aliases.Boolean:
                return AsField(FieldType.Boolean);
            case Constants.PropertyEditors.Aliases.Tags:
                return AsField(FieldType.String, many: true);
            case Constants.PropertyEditors.Aliases.ContentPicker:
                return AsField(FieldType.EntityReference, references: CmsDocumentDefinition.Type);
            case Constants.PropertyEditors.Aliases.BlockList:
            case Constants.PropertyEditors.Aliases.BlockGrid:
                return await MapBlockEditorAsync(propertyType, description, varies);
            default:
                return await MapByEditorAsync(propertyType, description, varies);
        }
    }

    /// <summary>
    /// Any other editor defines its own value model. Its handler, when one is registered for the editor, owns the paths
    /// inside the value; otherwise the value is set whole, described by the editor's own value schema when it has one
    /// (<see cref="IPropertyEditorSchemaService"/>), and checked by Umbraco when saved.
    /// </summary>
    private async Task<PropertyMap> MapByEditorAsync(IPropertyType propertyType, string description, ContentVariation varies)
    {
        var editor = propertyType.PropertyEditorAlias;
        var dataType = await dataTypes.GetAsync(propertyType.DataTypeKey);
        System.Text.Json.Nodes.JsonObject? schema = null;
        try
        {
            schema = schemas?.GetValueSchema(editor, dataType?.ConfigurationObject);
        }
        catch (Exception)
        {
            // An editor whose schema can't be built is treated like one without a schema.
        }

        var handler = editorHandlers.GetValueOrDefault(editor);
        if (schema is null && handler is null)
        {
            description = $"{description} A value in the '{editor}' editor's own format.";
        }

        return new PropertyMap(propertyType.Alias, editor, PropertyKind.Field, new FieldDefinition(propertyType.Alias, description, FieldType.Json, propertyType.Mandatory)
        {
            Variations = varies,
            Schema = schema,
            Handler = handler,
        }, []);
    }

    private async Task<PropertyMap> MapBlockEditorAsync(IPropertyType propertyType, string description, ContentVariation varies)
    {
        var dataType = await dataTypes.GetAsync(propertyType.DataTypeKey)
            ?? throw new InvalidOperationException($"No data type {propertyType.DataTypeKey} for property '{propertyType.Alias}'.");
        var rootGroup = dataType.Key.ToString();
        var areaGroup = $"{rootGroup}/areas";
        var isGrid = propertyType.PropertyEditorAlias == Constants.PropertyEditors.Aliases.BlockGrid;

        var blocks = new List<BlockMap>();
        FieldDefinition field;
        if (isGrid)
        {
            var config = dataType.ConfigurationObject as BlockGridConfiguration ?? new BlockGridConfiguration();
            var columns = config.GridColumns ?? 12;
            var layout = ElementShape.Of(
                new FieldDefinition("columnSpan", $"Columns the block spans, 1 to {columns}.", FieldType.Integer, Required: true),
                new FieldDefinition("rowSpan", "Rows the block spans.", FieldType.Integer));

            foreach (var block in config.Blocks)
            {
                var areas = block.Areas
                    .Select(a => new AreaMap(a.Key, a.Alias ?? a.Key.ToString()))
                    .ToList();
                var areaFields = block.Areas.Select(a => FieldDefinition.ItemsOf(a.Alias ?? a.Key.ToString(), $"Area '{a.Alias}'.", areaGroup) with
                {
                    Min = a.MinAllowed,
                    Max = a.MaxAllowed,
                    ItemLayout = layout,
                }).ToArray();

                blocks.Add(await RegisterBlockAsync(dataType.Key, block.ContentElementTypeKey, block.SettingsElementTypeKey, areas, areaFields,
                    group: block.AllowAtRoot || !block.AllowInAreas ? rootGroup : areaGroup,
                    alsoIn: block.AllowAtRoot && block.AllowInAreas ? [areaGroup] : []));
            }

            field = FieldDefinition.ItemsOf(propertyType.Alias, description, rootGroup) with
            {
                Variations = varies,
                Min = config.ValidationLimit.Min ?? (propertyType.Mandatory ? 1 : null),
                Max = config.ValidationLimit.Max,
                ItemLayout = layout,
            };
        }
        else
        {
            var config = dataType.ConfigurationObject as BlockListConfiguration ?? new BlockListConfiguration();
            foreach (var block in config.Blocks)
            {
                blocks.Add(await RegisterBlockAsync(dataType.Key, block.ContentElementTypeKey, block.SettingsElementTypeKey, [], [], rootGroup, []));
            }

            field = FieldDefinition.ItemsOf(propertyType.Alias, description, rootGroup) with
            {
                Variations = varies,
                Min = config.ValidationLimit.Min ?? (propertyType.Mandatory ? 1 : null),
                Max = config.ValidationLimit.Max,
            };
        }

        var kind = isGrid ? PropertyKind.BlockGrid : PropertyKind.BlockList;
        return new PropertyMap(propertyType.Alias, propertyType.PropertyEditorAlias, kind, field, blocks);
    }

    private async Task<BlockMap> RegisterBlockAsync(
        Guid dataTypeKey,
        Guid contentKey,
        Guid? settingsKey,
        IReadOnlyList<AreaMap> areas,
        FieldDefinition[] areaFields,
        string group,
        IReadOnlyList<string> alsoIn)
    {
        var content = RequireType(contentKey);
        var block = new BlockMap(contentKey, content.Alias, settingsKey, areas);

        // A block editor nested inside its own element type would recurse forever; its item types are
        // already being registered by the outer call.
        if (!_dataTypesInProgress.Add(dataTypeKey.Xor(contentKey)))
        {
            return block;
        }

        try
        {
            var shape = (await MapAsync(content, isDocument: false)).Shape;
            var settings = settingsKey is { } key ? (await MapAsync(RequireType(key), isDocument: false)).Shape : null;

            itemTypes.AddIfMissing(new ItemTypeDefinition(group, content.Alias, content.Name ?? content.Alias, content.Description ?? string.Empty,
                shape with { Fields = [.. shape.Fields, .. areaFields] })
            {
                SettingsShape = settings,
                AlsoIn = alsoIn,
            });
        }
        finally
        {
            _dataTypesInProgress.Remove(dataTypeKey.Xor(contentKey));
        }

        return block;
    }
}

internal static class GuidExtensions
{
    /// <summary>Combines two keys, only to name a (data type, element type) pair.</summary>
    public static Guid Xor(this Guid a, Guid b)
    {
        var x = a.ToByteArray();
        var y = b.ToByteArray();
        for (var i = 0; i < x.Length; i++)
        {
            x[i] ^= y[i];
        }

        return new Guid(x);
    }
}
