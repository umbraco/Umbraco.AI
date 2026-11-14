using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

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

namespace Umbraco.Cms.DraftSets;

/// <summary>A property value in editor format (what the Management API sends), per culture and segment.</summary>
public sealed record DraftValue(string Alias, string? Culture, string? Segment, JsonNode? Value)
{
    internal string Id => $"{Alias}|{Culture}|{Segment}";
}

/// <summary>A document as a draft set sees it: names and property values in editor format.</summary>
public sealed class DraftContent
{
    public required Guid Key { get; init; }

    public required Guid ContentTypeKey { get; init; }

    public Guid? ParentKey { get; init; }

    /// <summary>Names by culture; the invariant name under <c>""</c>.</summary>
    public Dictionary<string, string> Names { get; init; } = [];

    public List<DraftValue> Values { get; init; } = [];

    /// <summary>The live version this was read from (the update date's ticks); null for content the draft set creates.</summary>
    public long? Version { get; init; }

    public bool IsNew => Version is null;

    internal DraftContent Clone() => new()
    {
        Key = Key,
        ContentTypeKey = ContentTypeKey,
        ParentKey = ParentKey,
        Names = new(Names),
        Values = Values.Select(v => v with { Value = v.Value?.DeepClone() }).ToList(),
        Version = Version,
    };
}

/// <summary>A value both the draft set and someone else changed, differently, since the draft set started editing it.</summary>
/// <param name="Alias">The property alias, or <see cref="DraftSetService.NameAlias"/> for the name.</param>
public sealed record DraftConflict(string Alias, string? Culture, string? Segment, JsonNode? Base, JsonNode? Draft, JsonNode? Live);

/// <summary>A document someone else changed since the draft set started editing it.</summary>
public sealed record DraftRebase(Guid ContentKey, bool DeletedLive, IReadOnlyList<DraftValue> ChangedSince, IReadOnlyList<DraftConflict> Conflicts);

public enum DraftKeep
{
    Draft,
    Live,
}

public sealed class DraftSet
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public bool Applied { get; internal set; }

    public IReadOnlyCollection<Guid> ContentKeys => Entries.Keys;

    internal Dictionary<Guid, DraftEntry> Entries { get; } = [];
}

internal sealed class DraftEntry
{
    /// <summary>Content the draft set creates: all of it, as staged.</summary>
    public DraftContent? Created { get; set; }

    /// <summary>For existing content: the live content when the draft set first changed it, to see what changed since.</summary>
    public DraftContent? Base { get; init; }

    /// <summary>For existing content: only the values the draft set changes, by <see cref="DraftValue.Id"/>. Names are values with <see cref="DraftSetService.NameAlias"/>.</summary>
    public Dictionary<string, DraftValue> Changes { get; } = [];

    /// <summary>Conflicts resolved by keeping the draft: the live value it was kept over, by id.</summary>
    public Dictionary<string, JsonNode?> KeptOver { get; } = [];
}

/// <summary>
/// Groups content changes into a named set held apart from the live site: read through it, check it, then apply it all
/// at once or discard it. It records only what changed, per property, culture and segment, and reads apply those changes
/// over the live content. So changes someone else saves meanwhile are kept (rebased onto) unless they touch the same value,
/// which is a conflict to resolve. Nothing here knows about abilities or AI.
/// </summary>
public sealed class DraftSetService(
    IContentEditingService editing,
    PropertyEditorCollection editors,
    IJsonSerializer json,
    ICoreScopeProvider scopes)
{
    /// <summary>The alias a name has in <see cref="DraftValue"/>s and conflicts.</summary>
    public const string NameAlias = "#name";

    private static readonly JsonSerializerOptions ModelValueOptions = new() { Converters = { new JsonObjectConverter() } };

    private readonly ConcurrentDictionary<Guid, DraftSet> _sets = new();

    public DraftSet Create(string name)
    {
        var set = new DraftSet { Name = name };
        _sets[set.Id] = set;
        return set;
    }

    public DraftSet? Get(Guid id) => _sets.GetValueOrDefault(id);

    public void Discard(Guid id) => _sets.TryRemove(id, out _);

    // ── Recording ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Stages new content, whole. The caller chooses its key, so other changes can refer to it before it exists.</summary>
    public void StageNew(Guid id, DraftContent content)
    {
        var set = Open(id);
        set.Entries[content.Key] = new DraftEntry { Created = content.Clone() };
    }

    /// <summary>
    /// Stages changes to existing content: only the values given, compared with the content when the draft set first
    /// changed it. A value set back to what it was is no longer a change.
    /// </summary>
    public async Task StageChangesAsync(Guid id, Guid key, IEnumerable<DraftValue> values)
    {
        var set = Open(id);
        if (!set.Entries.TryGetValue(key, out var entry))
        {
            var live = await GetLiveAsync(key) ?? throw new InvalidOperationException($"No content {key}.");
            entry = new DraftEntry { Base = live };
            set.Entries[key] = entry;
        }

        if (entry.Created is { } created)
        {
            foreach (var value in values)
            {
                Overlay(created, value);
            }

            return;
        }

        foreach (var value in values)
        {
            var original = ValueIn(entry.Base!, value);
            if (JsonNode.DeepEquals(original, value.Value))
            {
                entry.Changes.Remove(value.Id);
            }
            else
            {
                entry.Changes[value.Id] = value with { Value = value.Value?.DeepClone() };
            }
        }
    }

    // ── Reading ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The live content in editor format, as the backoffice loads it.</summary>
    public async Task<DraftContent?> GetLiveAsync(Guid key)
    {
        var content = await editing.GetAsync(key);
        if (content is null)
        {
            return null;
        }

        var names = content.ContentType.VariesByCulture()
            ? content.AvailableCultures.ToDictionary(c => c, c => content.GetCultureName(c) ?? string.Empty)
            : new Dictionary<string, string> { [string.Empty] = content.Name ?? string.Empty };

        var values = new List<DraftValue>();
        foreach (var property in content.Properties)
        {
            foreach (var value in property.Values)
            {
                if (ReadEditorValue(property, value.Culture, value.Segment) is { } node)
                {
                    values.Add(new DraftValue(property.Alias, value.Culture, value.Segment, node));
                }
            }
        }

        return new DraftContent { Key = key, ContentTypeKey = content.ContentType.Key, Names = names, Values = values, Version = content.UpdateDate.Ticks };
    }

    /// <summary>The content as the draft set would leave it: live with the set's changes over it, or the set's new content.</summary>
    public async Task<DraftContent?> GetAsync(Guid id, Guid key)
    {
        var set = _sets.GetValueOrDefault(id);
        if (set is null || set.Applied || !set.Entries.TryGetValue(key, out var entry))
        {
            return await GetLiveAsync(key);
        }

        if (entry.Created is { } created)
        {
            return created.Clone();
        }

        var live = await GetLiveAsync(key);
        if (live is null)
        {
            return null;
        }

        foreach (var change in entry.Changes.Values)
        {
            Overlay(live, change);
        }

        return live;
    }

    // ── Checking ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Documents someone else changed since the set started editing them, what they changed, and where that conflicts.</summary>
    public async Task<IReadOnlyList<DraftRebase>> GetRebaseAsync(Guid id)
    {
        var result = new List<DraftRebase>();
        foreach (var (key, entry) in Open(id).Entries.Where(e => e.Value.Base is not null))
        {
            var live = await GetLiveAsync(key);
            if (live is null)
            {
                result.Add(new DraftRebase(key, DeletedLive: true, [], []));
                continue;
            }

            if (live.Version == entry.Base!.Version)
            {
                continue;
            }

            var changedSince = AllValues(entry.Base).Concat(AllValues(live))
                .DistinctBy(v => v.Id)
                .Where(v => !JsonNode.DeepEquals(ValueIn(entry.Base, v), ValueIn(live, v)))
                .Select(v => v with { Value = ValueIn(live, v) })
                .ToList();

            var conflicts = new List<DraftConflict>();
            foreach (var change in entry.Changes.Values)
            {
                var original = ValueIn(entry.Base, change);
                var theirs = ValueIn(live, change);
                var keptOver = entry.KeptOver.TryGetValue(change.Id, out var kept) && JsonNode.DeepEquals(kept, theirs);
                if (!JsonNode.DeepEquals(original, theirs) && !JsonNode.DeepEquals(change.Value, theirs) && !keptOver)
                {
                    conflicts.Add(new DraftConflict(change.Alias, change.Culture, change.Segment, original, change.Value, theirs));
                }
            }

            result.Add(new DraftRebase(key, DeletedLive: false, changedSince, conflicts));
        }

        return result;
    }

    /// <summary>Keeps the draft set's value or the live one for a conflict.</summary>
    public async Task ResolveAsync(Guid id, Guid key, string alias, string? culture, string? segment, DraftKeep keep)
    {
        var entry = Open(id).Entries.GetValueOrDefault(key) ?? throw new InvalidOperationException($"The draft set has no changes to {key}.");
        var change = new DraftValue(alias, culture, segment, null);
        if (keep == DraftKeep.Live)
        {
            entry.Changes.Remove(change.Id);
            return;
        }

        entry.KeptOver[change.Id] = ValueIn((await GetLiveAsync(key))!, change)?.DeepClone();
    }

    /// <summary>Umbraco's own validation of every document in the set, as it would be saved.</summary>
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PropertyValidationError>>> ValidateAsync(Guid id, Guid userKey)
    {
        var result = new Dictionary<Guid, IReadOnlyList<PropertyValidationError>>();
        foreach (var key in Open(id).ContentKeys)
        {
            var content = (await GetAsync(id, key))!;
            var attempt = content.IsNew
                ? await editing.ValidateCreateAsync(ToCreateModel(content), userKey)
                : await editing.ValidateUpdateAsync(key, new ValidateContentUpdateModel { Variants = Variants(content), Properties = Properties(content), Cultures = Cultures(content) }, userKey);
            result[key] = attempt.Result.ValidationErrors.ToList();
        }

        return result;
    }

    // ── Finishing ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves every document in the set in one scope: all or nothing. Umbraco saves invalid values and reports them
    /// afterwards, so any reported problem fails the apply and the scope rolls back.
    /// </summary>
    public async Task ApplyAsync(Guid id, Guid userKey)
    {
        var set = Open(id);
        using var scope = scopes.CreateCoreScope();
        foreach (var key in set.ContentKeys)
        {
            var content = (await GetAsync(id, key))!;
            ContentEditingOperationStatus status;
            ContentValidationResult validation;
            if (content.IsNew)
            {
                var created = await editing.CreateAsync(ToCreateModel(content), userKey);
                (status, validation) = (created.Status, created.Result.ValidationResult);
            }
            else
            {
                var updated = await editing.UpdateAsync(key, new ContentUpdateModel { Variants = Variants(content), Properties = Properties(content) }, userKey);
                (status, validation) = (updated.Status, updated.Result.ValidationResult);
            }

            // Umbraco reports problems in every culture; only those in cultures the document has (or invariant) fail it.
            var cultures = Cultures(content);
            var errors = validation.ValidationErrors.Where(e => e.Culture is null || cultures is null || cultures.Contains(e.Culture)).ToList();
            if (status is not (ContentEditingOperationStatus.Success or ContentEditingOperationStatus.PropertyValidationError) || errors.Count > 0)
            {
                var details = string.Join("; ", errors.Select(e => $"{e.Alias}{(e.Culture is null ? string.Empty : $" ({e.Culture})")}: {string.Join(" ", e.ErrorMessages)}"));
                throw new InvalidOperationException($"Umbraco did not save {content.Names.Values.FirstOrDefault() ?? key.ToString()}: {status}. {details}".Trim());
            }
        }

        scope.Complete();
        set.Applied = true;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────

    private DraftSet Open(Guid id)
    {
        var set = _sets.GetValueOrDefault(id) ?? throw new InvalidOperationException($"No draft set {id}.");
        return set.Applied ? throw new InvalidOperationException($"Draft set {id} is already applied.") : set;
    }

    /// <summary>The value in editor format, as the backoffice loads it: through the property editor's ToEditor.</summary>
    private JsonNode? ReadEditorValue(IProperty property, string? culture, string? segment)
    {
        var value = editors.TryGet(property.PropertyType.PropertyEditorAlias, out var editor)
            ? editor.GetValueEditor().ToEditor(property, culture, segment)
            : property.GetValue(culture, segment);
        return value switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            _ => JsonNode.Parse(json.Serialize(value)),
        };
    }

    private static IEnumerable<DraftValue> AllValues(DraftContent content)
        => content.Names.Select(n => new DraftValue(NameAlias, n.Key.Length == 0 ? null : n.Key, null, n.Value)).Concat(content.Values);

    private static JsonNode? ValueIn(DraftContent content, DraftValue value)
        => value.Alias == NameAlias
            ? content.Names.TryGetValue(value.Culture ?? string.Empty, out var name) ? JsonValue.Create(name) : null
            : content.Values.FirstOrDefault(v => v.Id == value.Id)?.Value;

    private static void Overlay(DraftContent content, DraftValue change)
    {
        if (change.Alias == NameAlias)
        {
            if (change.Value is null)
            {
                content.Names.Remove(change.Culture ?? string.Empty);
            }
            else
            {
                content.Names[change.Culture ?? string.Empty] = change.Value.GetValue<string>();
            }

            return;
        }

        content.Values.RemoveAll(v => v.Id == change.Id);
        if (change.Value is not null)
        {
            content.Values.Add(change with { Value = change.Value.DeepClone() });
        }
    }

    private static ContentCreateModel ToCreateModel(DraftContent content) => new()
    {
        Key = content.Key,
        ContentTypeKey = content.ContentTypeKey,
        ParentKey = content.ParentKey,
        Variants = Variants(content),
        Properties = Properties(content),
    };

    /// <summary>The cultures the document has, so validation doesn't fail cultures it doesn't exist in; null when invariant.</summary>
    private static HashSet<string>? Cultures(DraftContent content)
        => content.Names.Keys.Any(c => c.Length > 0) ? content.Names.Keys.Where(c => c.Length > 0).ToHashSet() : null;

    private static List<VariantModel> Variants(DraftContent content)
        => content.Names.Select(n => new VariantModel { Culture = n.Key.Length == 0 ? null : n.Key, Name = n.Value }).ToList();

    // Templates aren't carried yet: a real implementation re-sends the current template key, because a null TemplateKey clears it.
    private static List<PropertyValueModel> Properties(DraftContent content)
        => content.Values.Select(v => new PropertyValueModel
        {
            Alias = v.Alias,
            Culture = v.Culture,
            Segment = v.Segment,
            Value = v.Value is null ? null : JsonSerializer.Deserialize<object?>(v.Value.ToJsonString(), ModelValueOptions),
        }).ToList();
}
