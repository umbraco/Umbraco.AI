using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentEditing.Validation;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.DraftSets;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Extensions;

namespace Umbraco.Abilities.Cms;

/// <summary>
/// The real entity definition for CMS documents. It is also the adapter to CMS Draft Sets: inside a change set, content
/// changes are staged into a CMS draft set (per property) instead of being kept and replayed by the change set, so the
/// draft set is the source of truth for content and its own rebasing, review and apply are used.
/// <para>
/// The real entity definition for CMS documents. One entity type, <c>cms.document</c>, whose subtypes are the
/// document types. It reads through <see cref="IContentEditingService"/> in the editor (Management API) format,
/// maps block editor values to slots and back, validates with Umbraco's own validation, and saves with
/// <see cref="ContentCreateModel"/> / <see cref="ContentUpdateModel"/>.
/// </para>
/// </summary>
public sealed class CmsDocumentDefinition : IDraftingEntityDefinition
{
    public const string Type = "cms.document";

    // A block's settings key, kept on the item so a save reuses it.
    private const string SettingsKeyExtra = "settingsKey";

    private static readonly JsonSerializerOptions ModelValueOptions = new() { Converters = { new JsonObjectConverter() } };
    // Umbraco points into a block value either by index, ".contentData[0].values[2].value", or, for a value
    // that is missing, by filter: ".contentData[1].values[?(@.alias == 'heading' && @.culture == 'en-US' && @.segment == null)].value".
    private static readonly Regex BlockValuePath = new(
        @"^\.?(?<data>contentData|settingsData)\[(?<item>\d+)\]\.values\[(?:(?<value>\d+)|\?\(@\.alias == '(?<alias>[^']+)' && @\.culture == (?:'(?<culture>[^']*)'|null) && @\.segment == (?:'(?<segment>[^']*)'|null)\))\]\.value",
        RegexOptions.Compiled);

    private readonly IContentTypeService _contentTypes;
    private readonly ILanguageService _languages;
    private readonly IContentEditingService _editing;
    private readonly IEntityService _entities;
    private readonly DraftSetService _draftSets;
    private readonly IJsonSerializer _json;
    private readonly Func<Guid> _actingUser;
    private readonly CmsStructureMapper _mapper;

    public CmsDocumentDefinition(
        IContentTypeService contentTypes,
        IDataTypeService dataTypes,
        ILanguageService languages,
        IContentEditingService editing,
        IEntityService entities,
        DraftSetService draftSets,
        IJsonSerializer json,
        ItemTypeRegistry itemTypes,
        IUnitOfWorkProvider unitOfWork,
        Func<Guid> actingUser)
    {
        UnitOfWork = unitOfWork;
        _contentTypes = contentTypes;
        _languages = languages;
        _editing = editing;
        _entities = entities;
        _draftSets = draftSets;
        _json = json;
        _actingUser = actingUser;
        _mapper = new CmsStructureMapper(contentTypes, dataTypes, itemTypes);
    }

    public string EntityType => Type;

    public bool CanCreate => true;

    public IUnitOfWorkProvider UnitOfWork { get; }

    public Task<IReadOnlyList<EntitySubtype>> GetSubtypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntitySubtype>>(_contentTypes.GetAll()
            .Where(t => !t.IsElement)
            .Select(t => new EntitySubtype(t.Alias, t.Name ?? t.Alias))
            .ToList());

    public async Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
    {
        var type = DocumentType(subtype);
        var map = await _mapper.MapAsync(type, isDocument: true);
        var cultures = (await _languages.GetAllAsync()).Select(l => l.IsoCode).ToList();
        return new EntityStructure(Type, type.Name ?? type.Alias, type.Description ?? string.Empty, map.Shape)
        {
            Variations = map.Variations,
            Cultures = cultures,
        };
    }

    public EntityDocument New(Guid key, string? subtype) => new() { EntityType = Type, Key = key, Subtype = subtype };

    public async Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default)
        => await _draftSets.GetLiveAsync(key) is { } live ? await ToDocumentAsync(live) : null;

    /// <summary>A document in editor format, live or as a draft set would leave it, as an entity document.</summary>
    private async Task<EntityDocument> ToDocumentAsync(DraftContent content)
    {
        var type = _mapper.RequireType(content.ContentTypeKey);
        var map = await _mapper.MapAsync(type, isDocument: true);
        var document = new EntityDocument
        {
            EntityType = Type,
            Key = content.Key,
            Subtype = type.Alias,
            LiveVersion = content.Version,
        };

        var root = document.Root;
        foreach (var (culture, name) in content.Names)
        {
            root.Variant(new VariantId(culture.Length == 0 ? null : culture, null)).Fields[CmsStructureMapper.NameField] = name;
        }

        if (content.ParentKey is { } parent)
        {
            root.Fields[CmsStructureMapper.ParentField] = parent.ToString();
        }

        foreach (var value in content.Values)
        {
            if (!map.Properties.TryGetValue(value.Alias, out var propertyMap) || value.Value is not { } node)
            {
                continue;
            }

            var variant = new VariantId(value.Culture, value.Segment);
            if (propertyMap.Field is { } field)
            {
                root.Set(field, variant, node);
            }
            else
            {
                root.Items(propertyMap.Slot!, variant).AddRange(await ReadBlocksAsync(node, propertyMap));
            }
        }

        return document;
    }

    public async Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        var model = await BuildModelAsync(document);
        var attempt = document.LiveVersion is null
            ? await _editing.ValidateCreateAsync(model.ToCreateModel(), _actingUser())
            : await _editing.ValidateUpdateAsync(document.Key, model.ToValidateUpdateModel(), _actingUser());

        var problems = attempt.Result.ValidationErrors.Select(model.Index.ToProblem).ToList();
        if (!attempt.Success && attempt.Status != ContentEditingOperationStatus.PropertyValidationError)
        {
            problems.Add(new Problem(string.Empty, $"Umbraco refused the document: {attempt.Status}."));
        }

        return problems;
    }

    public async Task<EntityRef> SaveAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        var model = await BuildModelAsync(document);
        ContentEditingOperationStatus status;
        ContentValidationResult validation;

        if (document.LiveVersion is null)
        {
            var created = await _editing.CreateAsync(model.ToCreateModel(), _actingUser());
            (status, validation) = (created.Status, created.Result.ValidationResult);
        }
        else
        {
            var updated = await _editing.UpdateAsync(document.Key, model.ToUpdateModel(), _actingUser());
            (status, validation) = (updated.Status, updated.Result.ValidationResult);
        }

        // Umbraco saves invalid property values and reports them afterwards. A change set must not apply
        // half-valid content, so any reported problem fails the save and the surrounding scope rolls back.
        // Umbraco reports problems in every culture; only those in cultures the document has (or invariant) fail it.
        var cultures = model.Variants.Select(v => v.Culture).ToHashSet();
        var errors = validation.ValidationErrors.Where(e => e.Culture is null || cultures.Contains(null) || cultures.Contains(e.Culture)).ToList();
        if (status is not (ContentEditingOperationStatus.Success or ContentEditingOperationStatus.PropertyValidationError) || errors.Count > 0)
        {
            var details = string.Join("; ", errors.Select(model.Index.ToProblem).Select(p => $"{p.Path}{(p.Culture is null ? "" : $" ({p.Culture})")}: {p.Message}"));
            throw new InvalidOperationException($"Umbraco did not save {ToRef(document).Name}: {status}. {details}".Trim());
        }

        return ToRef(document);
    }

    public Task<IReadOnlyList<EntityRef>> FindAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntityRef>>(_entities.GetAll(UmbracoObjectTypes.Document)
            .Where(e => e.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .Select(e => new EntityRef(Type, e.Key, e.Name!, EditUrl(e.Key)))
            .ToList());

    public EntityRef ToRef(EntityDocument document)
        => new(Type, document.Key, document.Root.ReadAny(CmsStructureMapper.NameField)?.GetValue<string>() ?? "(unnamed)", EditUrl(document.Key));

    // ── CMS Draft Sets: the change set hands content changes to a CMS draft set ─────────────────────

    public Task<Guid> CreateDraftAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_draftSets.Create(name).Id);

    public async Task<EntityDocument?> LoadDraftAsync(Guid draftId, Guid key, CancellationToken cancellationToken = default)
        => await _draftSets.GetAsync(draftId, key) is { } content ? await ToDocumentAsync(content) : null;

    /// <summary>
    /// Stages an edit. New content is staged whole. For existing content only the values the edit changed are staged,
    /// per property, culture and segment, so the draft set can rebase onto values others change meanwhile.
    /// </summary>
    public async Task StageAsync(Guid draftId, EntityDocument? before, EntityDocument after, CancellationToken cancellationToken = default)
    {
        var model = await BuildModelAsync(after);
        if (before is null || before.LiveVersion is null)
        {
            _draftSets.StageNew(draftId, new DraftContent
            {
                Key = model.Key,
                ContentTypeKey = model.ContentTypeKey,
                ParentKey = model.ParentKey,
                Names = model.Variants.ToDictionary(v => v.Culture ?? string.Empty, v => v.Name),
                Values = model.Values,
            });
            return;
        }

        // What the edit changed, as the property values (or names) it touched: a change anywhere inside a block
        // property is a change to that property's value in its own variant.
        var type = DocumentType(after.Subtype);
        var map = await _mapper.MapAsync(type, isDocument: true);
        var touched = new HashSet<(string Alias, string? Culture, string? Segment)>();
        foreach (var change in EntityDiff.Compare(before.Root, after.Root))
        {
            var alias = change.Path.Split('/')[0];
            if (alias == CmsStructureMapper.NameField)
            {
                touched.Add((DraftSetService.NameAlias, type.VariesByCulture() ? change.Variant.Culture : null, null));
            }
            else if (map.Properties.TryGetValue(alias, out var property))
            {
                var variant = change.Variant.Narrow(property.Field?.Variations ?? property.Slot!.Variations);
                touched.Add((alias, variant.Culture, variant.Segment));
            }
        }

        var values = touched.Select(t => t.Alias == DraftSetService.NameAlias
            ? new DraftValue(t.Alias, t.Culture, null, model.Variants.FirstOrDefault(v => v.Culture == t.Culture)?.Name is { } name ? JsonValue.Create(name) : null)
            : model.Values.FirstOrDefault(v => v.Alias == t.Alias && v.Culture == t.Culture && v.Segment == t.Segment) ?? new DraftValue(t.Alias, t.Culture, t.Segment, null));
        await _draftSets.StageChangesAsync(draftId, after.Key, values);
    }

    public async Task<IReadOnlyList<RebasedEntity>> GetRebasedAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        var result = new List<RebasedEntity>();
        foreach (var rebase in await _draftSets.GetRebaseAsync(draftId))
        {
            var live = await LoadAsync(rebase.ContentKey, cancellationToken);
            var entity = live is not null ? ToRef(live) : new EntityRef(Type, rebase.ContentKey, "(deleted)", EditUrl(rebase.ContentKey));
            if (rebase.DeletedLive)
            {
                result.Add(new RebasedEntity(entity, ["(deleted)"], [new ChangeSetConflict(entity, string.Empty, null, null, null, null, null, null)]));
                continue;
            }

            result.Add(new RebasedEntity(
                entity,
                rebase.ChangedSince.Select(c => c.Culture is null ? PathOf(c.Alias) : $"{PathOf(c.Alias)} ({c.Culture})").Distinct().ToList(),
                rebase.Conflicts.Select(c => new ChangeSetConflict(entity, PathOf(c.Alias), c.Culture, c.Segment, c.Base, c.Draft, c.Live, null)).ToList()));
        }

        return result;
    }

    public Task ResolveConflictAsync(Guid draftId, Guid key, string path, VariantId variant, ConflictSide keep, CancellationToken cancellationToken = default)
        => _draftSets.ResolveAsync(
            draftId,
            key,
            path == CmsStructureMapper.NameField ? DraftSetService.NameAlias : path,
            variant.Culture,
            variant.Segment,
            keep == ConflictSide.Yours ? DraftKeep.Draft : DraftKeep.Live);

    public Task ApplyDraftAsync(Guid draftId, CancellationToken cancellationToken = default) => _draftSets.ApplyAsync(draftId, _actingUser());

    public Task DiscardDraftAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        _draftSets.Discard(draftId);
        return Task.CompletedTask;
    }

    private static string PathOf(string alias) => alias == DraftSetService.NameAlias ? CmsStructureMapper.NameField : alias;

    private static string EditUrl(Guid key) => $"/umbraco/section/content/workspace/document/edit/{key}";

    private IContentType DocumentType(string? alias)
        => _contentTypes.Get(alias ?? string.Empty) is { IsElement: false } type
            ? type
            : throw new AbilityInputException($"No document type '{alias}'.", [new Problem("subtype", "Not a document type.")]);

    private JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node.DeepClone(),
        _ => JsonNode.Parse(_json.Serialize(value)),
    };

    // ── Reading block values into slot items ────────────────────────────────────────────────────

    /// <summary>A block editor value, read with Umbraco's own block value classes, as slot items.</summary>
    private async Task<List<EntityItem>> ReadBlocksAsync(JsonNode node, PropertyMap property)
    {
        var json = node is JsonValue text && text.GetValueKind() == JsonValueKind.String ? text.GetValue<string>() : node.ToJsonString();
        BlockValue? value = property.Kind == PropertyKind.BlockGrid ? _json.Deserialize<BlockGridValue>(json) : _json.Deserialize<BlockListValue>(json);
        if (value is null)
        {
            return [];
        }

        var contentData = value.ContentData.ToDictionary(d => d.Key);
        var settingsData = value.SettingsData.ToDictionary(d => d.Key);

        var items = new List<EntityItem>();
        var layout = value.Layout.TryGetValue(property.EditorAlias, out var found) ? found : [];
        foreach (var layoutItem in layout.OfType<BlockLayoutItemBase>())
        {
            items.Add(await ReadItemAsync(layoutItem, property, contentData, settingsData));
        }

        return items;
    }

    private async Task<EntityItem> ReadItemAsync(BlockLayoutItemBase layoutItem, PropertyMap property, Dictionary<Guid, BlockItemData> contentData, Dictionary<Guid, BlockItemData> settingsData)
    {
        var content = contentData[layoutItem.ContentKey];
        var elementType = _mapper.RequireType(content.ContentTypeKey);
        var block = property.Blocks.First(b => b.ContentElementTypeKey == content.ContentTypeKey);

        var item = new EntityItem { Key = content.Key, Type = elementType.Alias };
        await ReadValuesAsync(item, content.Values, await _mapper.MapAsync(elementType, isDocument: false));

        if (layoutItem.SettingsKey is { } settingsKey
            && settingsData.TryGetValue(settingsKey, out var settings)
            && block.SettingsElementTypeKey is { } settingsType)
        {
            item.Extra[SettingsKeyExtra] = settingsKey.ToString();
            await ReadValuesAsync(item.EnsureSettings(), settings.Values, await _mapper.MapAsync(_mapper.RequireType(settingsType), isDocument: false));
        }

        if (layoutItem is BlockGridLayoutItem gridItem)
        {
            var placement = item.EnsureLayout();
            placement.Fields["columnSpan"] = gridItem.ColumnSpan;
            placement.Fields["rowSpan"] = gridItem.RowSpan;

            foreach (var area in gridItem.Areas ?? [])
            {
                var slot = item.Slot(block.Areas.FirstOrDefault(a => a.Key == area.Key)?.Alias ?? area.Key.ToString());
                foreach (var areaItem in area.Items ?? [])
                {
                    slot.Add(await ReadItemAsync(areaItem, property, contentData, settingsData));
                }
            }
        }

        return item;
    }

    private async Task ReadValuesAsync(EntityElement element, IEnumerable<BlockPropertyValue> values, ShapeMap map)
    {
        foreach (var value in values)
        {
            if (!map.Properties.TryGetValue(value.Alias, out var property) || ToNode(value.Value) is not { } node)
            {
                continue;
            }

            var variant = new VariantId(value.Culture, value.Segment);
            if (property.Field is { } field)
            {
                element.Set(field, variant, node);
            }
            else
            {
                element.Items(property.Slot!, variant).AddRange(await ReadBlocksAsync(node, property));
            }
        }
    }

    // ── Writing the working copy as a content model ────────────────────────────────────────────

    private async Task<ContentModel> BuildModelAsync(EntityDocument document)
    {
        var type = DocumentType(document.Subtype);
        var map = await _mapper.MapAsync(type, isDocument: true);
        var root = document.Root;
        var cultures = document.Variants().Select(v => v.Culture).OfType<string>().Distinct().ToList();
        var index = new ErrorIndex();

        var variants = new List<VariantModel>();
        if (type.VariesByCulture())
        {
            foreach (var culture in cultures)
            {
                if (root.VariantOrNull(new VariantId(culture, null))?.Fields.GetValueOrDefault(CmsStructureMapper.NameField) is { } name)
                {
                    variants.Add(new VariantModel { Culture = culture, Name = name.GetValue<string>() });
                }
            }
        }
        else
        {
            variants.Add(new VariantModel { Name = root.Fields.GetValueOrDefault(CmsStructureMapper.NameField)?.GetValue<string>() ?? string.Empty });
        }

        var values = new List<DraftValue>();
        foreach (var property in map.Properties.Values)
        {
            foreach (var (variant, value) in Values(root, property))
            {
                var node = await ToNodeAsync(value, property, cultures, variant.Culture, property.Alias, index.ForProperty(property.Alias, variant));
                values.Add(new DraftValue(property.Alias, variant.Culture, variant.Segment, node));
            }
        }

        var properties = values.Select(v => new PropertyValueModel { Alias = v.Alias, Value = ToModelValue(v.Value), Culture = v.Culture, Segment = v.Segment }).ToList();
        var parent = root.Fields.GetValueOrDefault(CmsStructureMapper.ParentField) is { } parentNode && Guid.TryParse(parentNode.ToString(), out var parentKey) ? parentKey : (Guid?)null;
        return new ContentModel(document.Key, type.Key, parent, variants, properties, values, index);
    }

    /// <summary>
    /// A property's values in the working copy: once if invariant, else once per variant that has one. A field's value
    /// is its JSON; a block property's is its slot items.
    /// </summary>
    private static IEnumerable<(VariantId Variant, object Value)> Values(EntityElement element, PropertyMap property)
    {
        var invariant = (property.Field?.Variations ?? property.Slot!.Variations) == Variation.Invariant;
        var values = property.Field is not null
            ? element.Values(property.Alias).Where(v => v.Value is not JsonArray { Count: 0 }).Select(v => (v.Variant, (object)v.Value))
            : element.SlotItems(property.Alias).Select(v => (v.Variant, (object)v.Items));
        return values.Where(v => v.Variant.IsInvariant == invariant);
    }

    /// <summary>A value in editor format: a field's JSON, or a block property's items as a block value.</summary>
    private async Task<JsonNode?> ToNodeAsync(object value, PropertyMap property, IReadOnlyList<string> documentCultures, string? envelopeCulture, string slotPath, EnvelopeIndex index)
        => value is JsonNode node
            ? node.DeepClone()
            : JsonNode.Parse(_json.Serialize(await WriteBlocksAsync((List<EntityItem>)value, property, documentCultures, envelopeCulture, slotPath, index)));

    /// <summary>Slot items as Umbraco's own block value: layout, content and settings data, and which cultures expose each block.</summary>
    private async Task<BlockValue> WriteBlocksAsync(List<EntityItem> items, PropertyMap property, IReadOnlyList<string> documentCultures, string? envelopeCulture, string slotPath, EnvelopeIndex index)
    {
        var contentData = new List<BlockItemData>();
        var settingsData = new List<BlockItemData>();
        var expose = new List<BlockItemVariation>();

        async Task<BlockLayoutItemBase> WriteItemAsync(EntityItem item, string path)
        {
            var block = property.BlockFor(item.Type)
                ?? throw new InvalidOperationException($"Block '{item.Type}' is not configured for '{property.Alias}'.");
            var elementMap = await _mapper.MapAsync(_mapper.RequireType(block.ContentElementTypeKey), isDocument: false);

            var values = await WriteValuesAsync(item, elementMap, documentCultures, envelopeCulture, path);
            contentData.Add(new BlockItemData { Key = item.Key, ContentTypeKey = block.ContentElementTypeKey, ContentTypeAlias = block.ContentAlias, Values = values });
            index.Content.Add(new IndexedElement(item.Key, path, values));

            // An element that varies by culture is exposed in each culture it has values for (or every culture
            // of the document when it has none yet); an invariant element is exposed once.
            List<string?> exposedIn = [envelopeCulture];
            if (elementMap.Variations.VariesByCulture())
            {
                var own = item.VariantIds.Select(v => v.Culture).OfType<string>().Distinct().Cast<string?>().ToList();
                exposedIn = own.Count > 0 ? own : envelopeCulture is not null ? [envelopeCulture] : documentCultures.Cast<string?>().ToList();
            }

            foreach (var culture in exposedIn)
            {
                expose.Add(new BlockItemVariation(item.Key, culture, null));
                index.Exposed.Add((item.Key, culture));
            }

            Guid? settingsKey = null;
            if (block.SettingsElementTypeKey is { } settingsType)
            {
                settingsKey = item.Extra.TryGetValue(SettingsKeyExtra, out var stored) ? Guid.Parse(stored) : DerivedKey(item.Key, "settings");
                var settingsMap = await _mapper.MapAsync(_mapper.RequireType(settingsType), isDocument: false);
                var settingsValues = await WriteValuesAsync(item.Settings ?? new EntityElement(), settingsMap, documentCultures, envelopeCulture, $"{path}/@settings");
                settingsData.Add(new BlockItemData { Key = settingsKey.Value, ContentTypeKey = settingsType, Values = settingsValues });
                index.Settings.Add(new IndexedElement(item.Key, $"{path}/@settings", settingsValues));
            }

            if (property.Kind != PropertyKind.BlockGrid)
            {
                return new BlockListLayoutItem { ContentKey = item.Key, SettingsKey = settingsKey };
            }

            var areas = new List<BlockGridLayoutAreaItem>();
            foreach (var area in block.Areas)
            {
                var areaItems = new List<BlockGridLayoutItem>();
                foreach (var areaItem in item.Slots.GetValueOrDefault(area.Alias) ?? [])
                {
                    areaItems.Add((BlockGridLayoutItem)await WriteItemAsync(areaItem, $"{path}/{area.Alias}/{areaItem.Key}"));
                }

                areas.Add(new BlockGridLayoutAreaItem(area.Key) { Items = [.. areaItems] });
            }

            return new BlockGridLayoutItem
            {
                ContentKey = item.Key,
                SettingsKey = settingsKey,
                ColumnSpan = IntOf(item.Layout?.Fields.GetValueOrDefault("columnSpan")),
                RowSpan = IntOf(item.Layout?.Fields.GetValueOrDefault("rowSpan")) ?? 1,
                Areas = [.. areas],
            };
        }

        var layout = new List<IBlockLayoutItem>();
        foreach (var item in items)
        {
            layout.Add(await WriteItemAsync(item, $"{slotPath}/{item.Key}"));
        }

        var blocks = new Dictionary<string, IEnumerable<IBlockLayoutItem>> { [property.EditorAlias] = layout };
        return property.Kind == PropertyKind.BlockGrid
            ? new BlockGridValue { Layout = blocks, ContentData = contentData, SettingsData = settingsData, Expose = expose }
            : new BlockListValue { Layout = blocks, ContentData = contentData, SettingsData = settingsData, Expose = expose };
    }

    private async Task<List<BlockPropertyValue>> WriteValuesAsync(EntityElement element, ShapeMap map, IReadOnlyList<string> documentCultures, string? envelopeCulture, string path)
    {
        var values = new List<BlockPropertyValue>();
        foreach (var property in map.Properties.Values)
        {
            foreach (var (variant, value) in Values(element, property))
            {
                values.Add(new BlockPropertyValue
                {
                    Alias = property.Alias,
                    Culture = variant.Culture,
                    Segment = variant.Segment,
                    Value = ToModelValue(await ToNodeAsync(value, property, documentCultures, variant.Culture ?? envelopeCulture, $"{path}/{property.Alias}", new EnvelopeIndex())),
                });
            }
        }

        return values;
    }

    private static int? IntOf(JsonNode? node) => node is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;

    private static object? ToModelValue(JsonNode? node)
        => node is null ? null : JsonSerializer.Deserialize<object?>(node.ToJsonString(), ModelValueOptions);

    /// <summary>A stable key derived from another, so new blocks get the same settings key on every replay.</summary>
    private static Guid DerivedKey(Guid key, string purpose)
        => new(MD5.HashData(Encoding.UTF8.GetBytes($"{key:N}/{purpose}")));

    private sealed record ContentModel(Guid Key, Guid ContentTypeKey, Guid? ParentKey, List<VariantModel> Variants, List<PropertyValueModel> Properties, List<DraftValue> Values, ErrorIndex Index)
    {
        public ContentCreateModel ToCreateModel() => new()
        {
            Key = Key,
            ContentTypeKey = ContentTypeKey,
            ParentKey = ParentKey,
            Variants = Variants,
            Properties = Properties,
        };

        // Templates aren't carried yet: the test document types have none. A real definition re-sends the
        // current template key, because a null TemplateKey clears it.
        public ContentUpdateModel ToUpdateModel() => new() { Variants = Variants, Properties = Properties };

        // Only the cultures the document has: validating all of them fails mandatory values in cultures it doesn't exist in.
        public ValidateContentUpdateModel ToValidateUpdateModel() => new()
        {
            Variants = Variants,
            Properties = Properties,
            Cultures = Variants.Any(v => v.Culture is not null) ? Variants.Select(v => v.Culture!).Where(c => c is not null).ToHashSet() : null,
        };
    }

    private sealed record IndexedElement(Guid ContentKey, string Path, IList<BlockPropertyValue> Values);

    private sealed class EnvelopeIndex
    {
        public List<IndexedElement> Content { get; } = [];

        public List<IndexedElement> Settings { get; } = [];

        public List<(Guid Key, string? Culture)> Exposed { get; } = [];
    }

    /// <summary>
    /// Turns Umbraco's validation errors (alias, culture, segment and a JSON path into the block value) back into
    /// the paths the entity abilities use. Umbraco indexes <c>contentData[i]</c> over the blocks exposed in the
    /// culture being validated, so the index replays that filter.
    /// </summary>
    private sealed class ErrorIndex
    {
        private readonly Dictionary<(string Alias, string? Culture, string? Segment), EnvelopeIndex> _envelopes = [];

        public EnvelopeIndex ForProperty(string alias, VariantId variant)
        {
            var index = new EnvelopeIndex();
            _envelopes[(alias, variant.Culture, variant.Segment)] = index;
            return index;
        }

        public Problem ToProblem(PropertyValidationError error)
        {
            var message = string.Join(" ", error.ErrorMessages);
            var located = new Problem(error.Alias, message) { Culture = error.Culture, Segment = error.Segment };
            var match = BlockValuePath.Match(error.JsonPath ?? string.Empty);
            if (!match.Success)
            {
                return string.IsNullOrEmpty(error.JsonPath) || error.JsonPath == "$"
                    ? located
                    : located with { Path = $"{error.Alias} {error.JsonPath}" };
            }

            if (!_envelopes.TryGetValue((error.Alias, error.Culture, error.Segment), out var envelope)
                && !_envelopes.TryGetValue((error.Alias, null, null), out envelope))
            {
                return located with { Path = $"{error.Alias} {error.JsonPath}" };
            }

            // Inside an invariant block property the culture is in the filter, not on the error.
            var culture = match.Groups["culture"].Success ? match.Groups["culture"].Value : error.Culture;
            var segment = match.Groups["segment"].Success ? match.Groups["segment"].Value : error.Segment;

            // contentData[i] counts only the blocks exposed in the culture being validated.
            var exposed = envelope.Exposed.Where(e => e.Culture == culture || e.Culture is null).Select(e => e.Key).ToHashSet();
            var elements = (match.Groups["data"].Value == "contentData" ? envelope.Content : envelope.Settings).Where(e => exposed.Contains(e.ContentKey)).ToList();
            var i = int.Parse(match.Groups["item"].Value);
            if (i >= elements.Count)
            {
                return located with { Path = $"{error.Alias} {error.JsonPath}" };
            }

            var field = match.Groups["alias"].Success
                ? match.Groups["alias"].Value
                : elements[i].Values.ElementAtOrDefault(int.Parse(match.Groups["value"].Value))?.Alias;
            return located with
            {
                Path = field is null ? elements[i].Path : $"{elements[i].Path}/{field}",
                Culture = culture,
                Segment = segment,
            };
        }
    }
}

/// <summary>Content's step of applying a change set runs in one Umbraco scope, so every document saves or none does.</summary>
public sealed class CmsScopeUnitOfWorkProvider(ICoreScopeProvider scopes) : IUnitOfWorkProvider
{
    public string Name => "Content";

    public IUnitOfWork Begin() => new Scope(scopes.CreateCoreScope());

    private sealed class Scope(ICoreScope scope) : IUnitOfWork
    {
        public void Complete() => scope.Complete();

        public void Dispose() => scope.Dispose();
    }
}
