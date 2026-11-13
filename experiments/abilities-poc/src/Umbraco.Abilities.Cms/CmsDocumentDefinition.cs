using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Abilities.DraftSets;
using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentEditing.Validation;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Extensions;

using Element = Umbraco.Abilities.Entities.Element;

namespace Umbraco.Abilities.Cms;

/// <summary>
/// The real entity definition for CMS documents. One entity type, <c>cms.document</c>, whose subtypes are the
/// document types. It reads through <see cref="IContentEditingService"/> in the editor (Management API) format,
/// maps block editor values to slots and back, validates with Umbraco's own validation, and saves with
/// <see cref="ContentCreateModel"/> / <see cref="ContentUpdateModel"/>.
/// </summary>
public sealed class CmsDocumentDefinition : IEntityDefinition
{
    public const string Type = "cms.document";

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
    private readonly PropertyEditorCollection _editors;
    private readonly IJsonSerializer _json;
    private readonly Func<Guid> _actingUser;
    private readonly CmsStructureMapper _mapper;

    public CmsDocumentDefinition(
        IContentTypeService contentTypes,
        IDataTypeService dataTypes,
        ILanguageService languages,
        IContentEditingService editing,
        IEntityService entities,
        PropertyEditorCollection editors,
        IJsonSerializer json,
        ItemTypeRegistry itemTypes,
        Func<Guid> actingUser)
    {
        _contentTypes = contentTypes;
        _languages = languages;
        _editing = editing;
        _entities = entities;
        _editors = editors;
        _json = json;
        _actingUser = actingUser;
        _mapper = new CmsStructureMapper(contentTypes, dataTypes, itemTypes);
    }

    public string EntityType => Type;

    public bool CanCreate => true;

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
            Varies = map.Varies,
            Cultures = cultures,
        };
    }

    public EntityDocument New(Guid key, string? subtype) => new() { EntityType = Type, Key = key, Subtype = subtype };

    public async Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var content = await _editing.GetAsync(key);
        if (content is null)
        {
            return null;
        }

        var type = _mapper.RequireType(content.ContentType.Key);
        var map = await _mapper.MapAsync(type, isDocument: true);
        var document = new EntityDocument
        {
            EntityType = Type,
            Key = key,
            Subtype = type.Alias,
            LiveVersion = content.UpdateDate.Ticks,
        };

        var root = document.Root;
        if (type.VariesByCulture())
        {
            foreach (var culture in content.AvailableCultures)
            {
                Element.Fields(Element.VariantPart(root, new VariantId(culture, null)))[CmsStructureMapper.NameField] = content.GetCultureName(culture);
            }
        }
        else
        {
            Element.Fields(root)[CmsStructureMapper.NameField] = content.Name;
        }

        foreach (var property in content.Properties)
        {
            if (!map.Properties.TryGetValue(property.Alias, out var propertyMap))
            {
                continue;
            }

            foreach (var value in property.Values)
            {
                var node = ReadEditorValue(property, value.Culture, value.Segment);
                if (node is null)
                {
                    continue;
                }

                var variant = new VariantId(value.Culture, value.Segment);
                if (propertyMap.Field is { } field)
                {
                    Element.FieldsFor(root, field, variant)[property.Alias] = node;
                }
                else
                {
                    var slot = Element.SlotFor(root, propertyMap.Slot!, variant);
                    foreach (var item in await ReadEnvelopeAsync(node, propertyMap))
                    {
                        slot.Add(item);
                    }
                }
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

        // Umbraco saves invalid property values and reports them afterwards. A draft set must not apply
        // half-valid content, so any reported problem fails the save and the surrounding scope rolls back.
        if (status != ContentEditingOperationStatus.Success || validation.ValidationErrors.Any())
        {
            var details = string.Join("; ", validation.ValidationErrors.Select(model.Index.ToProblem).Select(p => $"{p.Path}{(p.Culture is null ? "" : $" ({p.Culture})")}: {p.Message}"));
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
        => new(Type, document.Key, Element.ReadAny(document.Root, CmsStructureMapper.NameField)?.GetValue<string>() ?? "(unnamed)", EditUrl(document.Key));

    private static string EditUrl(Guid key) => $"/umbraco/section/content/workspace/document/edit/{key}";

    private IContentType DocumentType(string? alias)
        => _contentTypes.Get(alias ?? string.Empty) is { IsElement: false } type
            ? type
            : throw new AbilityInputException($"No document type '{alias}'.", [new Problem("subtype", "Not a document type.")]);

    /// <summary>The value in editor format, as the backoffice loads it: through the property editor's ToEditor.</summary>
    private JsonNode? ReadEditorValue(IProperty property, string? culture, string? segment)
    {
        if (!_editors.TryGet(property.PropertyType.PropertyEditorAlias, out var editor))
        {
            return property.GetValue(culture, segment) is { } stored ? JsonNode.Parse(_json.Serialize(stored)) : null;
        }

        return editor.GetValueEditor().ToEditor(property, culture, segment) switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            var value => JsonNode.Parse(_json.Serialize(value)),
        };
    }

    // ── Reading block values into slot items ────────────────────────────────────────────────────

    private async Task<List<JsonObject>> ReadEnvelopeAsync(JsonNode node, PropertyMap property)
    {
        if (node is JsonValue text && text.GetValueKind() == JsonValueKind.String)
        {
            node = JsonNode.Parse(text.GetValue<string>())!;
        }

        var layout = node["layout"]?[property.EditorAlias] as JsonArray ?? [];
        var contentData = Index(node["contentData"]);
        var settingsData = Index(node["settingsData"]);

        var items = new List<JsonObject>();
        foreach (var layoutItem in layout.OfType<JsonObject>())
        {
            items.Add(await ReadItemAsync(layoutItem, property, contentData, settingsData));
        }

        return items;
    }

    private async Task<JsonObject> ReadItemAsync(JsonObject layoutItem, PropertyMap property, Dictionary<Guid, JsonObject> contentData, Dictionary<Guid, JsonObject> settingsData)
    {
        var contentKey = Guid.Parse(layoutItem["contentKey"]!.ToString());
        var content = contentData[contentKey];
        var elementKey = Guid.Parse(content["contentTypeKey"]!.ToString());
        var elementType = _mapper.RequireType(elementKey);
        var block = property.Blocks.First(b => b.ContentElementTypeKey == elementKey);

        var item = Element.NewItem(contentKey, elementType.Alias);
        await ReadValuesAsync(item, content["values"] as JsonArray, await _mapper.MapAsync(elementType, isDocument: false));

        if (layoutItem["settingsKey"] is JsonNode settingsKeyNode
            && Guid.TryParse(settingsKeyNode.ToString(), out var settingsKey)
            && settingsData.TryGetValue(settingsKey, out var settings)
            && block.SettingsElementTypeKey is { } settingsType)
        {
            item["settingsKey"] = settingsKey.ToString();
            await ReadValuesAsync(Element.SubElement(item, "settings"), settings["values"] as JsonArray, await _mapper.MapAsync(_mapper.RequireType(settingsType), isDocument: false));
        }

        if (property.Kind == PropertyKind.BlockGrid)
        {
            var layoutFields = Element.Fields(Element.SubElement(item, "layout"));
            layoutFields["columnSpan"] = layoutItem["columnSpan"]?.DeepClone();
            layoutFields["rowSpan"] = layoutItem["rowSpan"]?.DeepClone();

            foreach (var area in (layoutItem["areas"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var areaKey = Guid.Parse(area["key"]!.ToString());
                var alias = block.Areas.FirstOrDefault(a => a.Key == areaKey)?.Alias ?? areaKey.ToString();
                var slot = Element.Slot(item, alias);
                foreach (var areaItem in (area["items"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    slot.Add(await ReadItemAsync(areaItem, property, contentData, settingsData));
                }
            }
        }

        return item;
    }

    private async Task ReadValuesAsync(JsonObject element, JsonArray? values, ShapeMap map)
    {
        foreach (var value in (values ?? []).OfType<JsonObject>())
        {
            var alias = value["alias"]!.GetValue<string>();
            if (!map.Properties.TryGetValue(alias, out var property) || value["value"] is not JsonNode node)
            {
                continue;
            }

            var variant = new VariantId(value["culture"]?.GetValue<string>(), value["segment"]?.GetValue<string>());
            if (property.Field is { } field)
            {
                Element.FieldsFor(element, field, variant)[alias] = node.DeepClone();
            }
            else
            {
                var slot = Element.SlotFor(element, property.Slot!, variant);
                foreach (var item in await ReadEnvelopeAsync(node, property))
                {
                    slot.Add(item);
                }
            }
        }
    }

    private static Dictionary<Guid, JsonObject> Index(JsonNode? data)
        => (data as JsonArray ?? []).OfType<JsonObject>().ToDictionary(d => Guid.Parse(d["key"]!.ToString()));

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
                if (Element.VariantPartOrNull(root, new VariantId(culture, null))?["fields"]?[CmsStructureMapper.NameField] is JsonNode name)
                {
                    variants.Add(new VariantModel { Culture = culture, Name = name.GetValue<string>() });
                }
            }
        }
        else
        {
            variants.Add(new VariantModel { Name = Element.Fields(root)[CmsStructureMapper.NameField]?.GetValue<string>() ?? string.Empty });
        }

        var properties = new List<PropertyValueModel>();
        foreach (var property in map.Properties.Values)
        {
            foreach (var (variant, node) in Values(root, property))
            {
                var value = property.Field is not null
                    ? node
                    : await WriteEnvelopeAsync((JsonArray)node, property, cultures, envelopeCulture: variant.Culture, property.Alias, index.ForProperty(property.Alias, variant));

                properties.Add(new PropertyValueModel
                {
                    Alias = property.Alias,
                    Value = ToModelValue(value),
                    Culture = variant.Culture,
                    Segment = variant.Segment,
                });
            }
        }

        var parent = Element.Fields(root)[CmsStructureMapper.ParentField] is JsonNode parentNode && Guid.TryParse(parentNode.ToString(), out var parentKey) ? parentKey : (Guid?)null;
        return new ContentModel(document.Key, type.Key, parent, variants, properties, index);
    }

    /// <summary>A property's values in the working copy: once if invariant, else once per variant that has one.</summary>
    private static IEnumerable<(VariantId Variant, JsonNode Value)> Values(JsonObject element, PropertyMap property)
    {
        var varies = property.Field?.Varies ?? property.Slot!.Varies;
        if (varies == Variance.Invariant)
        {
            var node = property.Field is not null ? Element.Fields(element)[property.Alias] : Element.Slot(element, property.Alias);
            if (node is not null && node is not JsonArray { Count: 0 })
            {
                yield return (VariantId.Invariant, node);
            }

            yield break;
        }

        foreach (var (key, part) in element["variants"] as JsonObject ?? [])
        {
            var node = property.Field is not null ? part?["fields"]?[property.Alias] : part?["slots"]?[property.Alias];
            if (node is not null && node is not JsonArray { Count: 0 })
            {
                yield return (VariantId.Parse(key), node);
            }
        }
    }

    private async Task<JsonObject> WriteEnvelopeAsync(JsonArray items, PropertyMap property, IReadOnlyList<string> documentCultures, string? envelopeCulture, string slotPath, EnvelopeIndex index)
    {
        var layout = new JsonArray();
        var contentData = new JsonArray();
        var settingsData = new JsonArray();
        var expose = new JsonArray();

        async Task<JsonObject> WriteItemAsync(JsonObject item, string path)
        {
            var key = Element.ItemKey(item);
            var block = property.BlockFor(Element.ItemType(item))
                ?? throw new InvalidOperationException($"Block '{Element.ItemType(item)}' is not configured for '{property.Alias}'.");
            var elementMap = await _mapper.MapAsync(_mapper.RequireType(block.ContentElementTypeKey), isDocument: false);

            var values = await WriteValuesAsync(item, elementMap, documentCultures, envelopeCulture, path);
            contentData.Add(new JsonObject { ["key"] = key.ToString(), ["contentTypeKey"] = block.ContentElementTypeKey.ToString(), ["values"] = values });
            index.Content.Add(new IndexedElement(key, path, values));

            // An element that varies by culture is exposed in each culture it has values for (or every culture
            // of the document when it has none yet); an invariant element is exposed once.
            List<string?> exposedIn = [envelopeCulture];
            if (elementMap.Varies.ByCulture())
            {
                var own = (item["variants"] as JsonObject ?? []).Select(v => VariantId.Parse(v.Key).Culture).OfType<string>().Distinct().Cast<string?>().ToList();
                exposedIn = own.Count > 0 ? own : envelopeCulture is not null ? [envelopeCulture] : documentCultures.Cast<string?>().ToList();
            }
            foreach (var culture in exposedIn)
            {
                expose.Add(new JsonObject { ["contentKey"] = key.ToString(), ["culture"] = culture, ["segment"] = null });
                index.Exposed.Add((key, culture));
            }

            var layoutItem = new JsonObject { ["contentKey"] = key.ToString() };
            if (block.SettingsElementTypeKey is { } settingsType)
            {
                var settingsKey = item["settingsKey"]?.GetValue<string>() ?? DerivedKey(key, "settings").ToString();
                var settingsMap = await _mapper.MapAsync(_mapper.RequireType(settingsType), isDocument: false);
                var settingsValues = await WriteValuesAsync(item["settings"] as JsonObject ?? Element.New(), settingsMap, documentCultures, envelopeCulture, $"{path}/@settings");
                settingsData.Add(new JsonObject { ["key"] = settingsKey, ["contentTypeKey"] = settingsType.ToString(), ["values"] = settingsValues });
                index.Settings.Add(new IndexedElement(key, $"{path}/@settings", settingsValues));
                layoutItem["settingsKey"] = settingsKey;
            }

            if (property.Kind == PropertyKind.BlockGrid)
            {
                var placement = item["layout"]?["fields"];
                layoutItem["columnSpan"] = placement?["columnSpan"]?.DeepClone();
                layoutItem["rowSpan"] = placement?["rowSpan"]?.DeepClone() ?? 1;

                var areas = new JsonArray();
                foreach (var area in block.Areas)
                {
                    var areaItems = new JsonArray();
                    foreach (var areaItem in Element.Slot(item, area.Alias).OfType<JsonObject>())
                    {
                        areaItems.Add(await WriteItemAsync(areaItem, $"{path}/{area.Alias}/{Element.ItemKey(areaItem)}"));
                    }

                    areas.Add(new JsonObject { ["key"] = area.Key.ToString(), ["items"] = areaItems });
                }

                layoutItem["areas"] = areas;
            }

            return layoutItem;
        }

        foreach (var item in items.OfType<JsonObject>())
        {
            layout.Add(await WriteItemAsync(item, $"{slotPath}/{Element.ItemKey(item)}"));
        }

        return new JsonObject
        {
            ["layout"] = new JsonObject { [property.EditorAlias] = layout },
            ["contentData"] = contentData,
            ["settingsData"] = settingsData,
            ["expose"] = expose,
        };
    }

    private async Task<JsonArray> WriteValuesAsync(JsonObject element, ShapeMap map, IReadOnlyList<string> documentCultures, string? envelopeCulture, string path)
    {
        var values = new JsonArray();
        foreach (var property in map.Properties.Values)
        {
            foreach (var (variant, node) in Values(element, property))
            {
                var value = property.Field is not null
                    ? node.DeepClone()
                    : await WriteEnvelopeAsync((JsonArray)node, property, documentCultures, variant.Culture ?? envelopeCulture, $"{path}/{property.Alias}", new EnvelopeIndex());

                values.Add(new JsonObject
                {
                    ["alias"] = property.Alias,
                    ["culture"] = variant.Culture,
                    ["segment"] = variant.Segment,
                    ["value"] = value,
                });
            }
        }

        return values;
    }

    private static object? ToModelValue(JsonNode? node)
        => node is null ? null : JsonSerializer.Deserialize<object?>(node.ToJsonString(), ModelValueOptions);

    /// <summary>A stable key derived from another, so new blocks get the same settings key on every replay.</summary>
    private static Guid DerivedKey(Guid key, string purpose)
        => new(MD5.HashData(Encoding.UTF8.GetBytes($"{key:N}/{purpose}")));

    private sealed record ContentModel(Guid Key, Guid ContentTypeKey, Guid? ParentKey, List<VariantModel> Variants, List<PropertyValueModel> Properties, ErrorIndex Index)
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

        public ValidateContentUpdateModel ToValidateUpdateModel() => new() { Variants = Variants, Properties = Properties };
    }

    private sealed record IndexedElement(Guid ContentKey, string Path, JsonArray Values);

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
                : elements[i].Values.ElementAtOrDefault(int.Parse(match.Groups["value"].Value))?["alias"]?.GetValue<string>();
            return located with
            {
                Path = field is null ? elements[i].Path : $"{elements[i].Path}/{field}",
                Culture = culture,
                Segment = segment,
            };
        }
    }
}

/// <summary>Applies a draft set inside one Umbraco scope, so every document saves or none does.</summary>
public sealed class CmsScopeUnitOfWorkProvider(ICoreScopeProvider scopes) : IUnitOfWorkProvider
{
    public IUnitOfWork Begin() => new Scope(scopes.CreateCoreScope());

    private sealed class Scope(ICoreScope scope) : IUnitOfWork
    {
        public void Complete() => scope.Complete();

        public void Dispose() => scope.Dispose();
    }
}
