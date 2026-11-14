using System.Text.Json.Nodes;

using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;

namespace Umbraco.Abilities.Samples;

/// <summary>
/// Stands in for one product's database and transaction. Each sample product has its own, as Commerce, Automate
/// and Umbraco.AI can each use their own connection string.
/// </summary>
public sealed class InMemoryDatabase(string name) : IUnitOfWorkProvider
{
    public string Name { get; } = name;

    private Dictionary<string, Dictionary<Guid, Row>> _tables = [];

    // Every saved version, for products that keep version history.
    private readonly Dictionary<(string EntityType, Guid Key, long Version), JsonObject> _history = [];

    public sealed record Row(JsonObject Root, long Version);

    public Row? Get(string entityType, Guid key)
        => _tables.TryGetValue(entityType, out var table) && table.TryGetValue(key, out var row)
            ? row with { Root = (JsonObject)row.Root.DeepClone() }
            : null;

    public IEnumerable<(Guid Key, Row Row)> All(string entityType)
        => _tables.TryGetValue(entityType, out var table) ? table.Select(kv => (kv.Key, kv.Value)).ToList() : [];

    public long Put(string entityType, Guid key, JsonObject root)
    {
        if (!_tables.TryGetValue(entityType, out var table))
        {
            table = [];
            _tables[entityType] = table;
        }

        var version = table.TryGetValue(key, out var existing) ? existing.Version + 1 : 1;
        table[key] = new Row((JsonObject)root.DeepClone(), version);
        _history[(entityType, key, version)] = (JsonObject)root.DeepClone();
        return version;
    }

    public Row? GetVersion(string entityType, Guid key, long version)
        => _history.TryGetValue((entityType, key, version), out var root) ? new Row((JsonObject)root.DeepClone(), version) : null;

    public void ForgetHistory()
    {
        _history.Clear();
    }

    public IUnitOfWork Begin() => new Transaction(this, Snapshot());

    private Dictionary<string, Dictionary<Guid, Row>> Snapshot()
        => _tables.ToDictionary(t => t.Key, t => t.Value.ToDictionary(r => r.Key, r => r.Value with { Root = (JsonObject)r.Value.Root.DeepClone() }));

    private sealed class Transaction(InMemoryDatabase database, Dictionary<string, Dictionary<Guid, Row>> snapshot) : IUnitOfWork
    {
        private bool _completed;

        public void Complete() => _completed = true;

        public void Dispose()
        {
            if (!_completed)
            {
                database._tables = snapshot;
            }
        }
    }
}

/// <summary>
/// Sample storage for a typed model: the model is kept in its document form. A real product loads and saves its own
/// model instead (Commerce's discount service, for example), and its definition is just as short.
/// </summary>
public abstract class InMemoryModelDefinition<TModel>(InMemoryDatabase database, ItemTypeRegistry itemTypes, string editUrlPrefix)
    : EntityDefinitionBase<TModel>(itemTypes, database)
    where TModel : class
{
    protected InMemoryDatabase Database { get; } = database;

    public override Task<TModel?> LoadModelAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(Database.Get(EntityType, key) is { } row ? ToModel(Document(key, row)) : null);

    public override Task SaveModelAsync(TModel model, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(model);
        Database.Put(EntityType, document.Key, EntityJson.ToJson(document.Root));
        return Task.CompletedTask;
    }

    /// <summary>Every stored model, for checks across entities such as unique codes.</summary>
    protected IEnumerable<TModel> All() => Database.All(EntityType).Select(r => ToModel(Document(r.Key, r.Row)));

    protected override Task<IReadOnlyList<TModel>> FindModelsAsync(string query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TModel>>(All().Where(m => ToRef(ToDocument(m)).Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());

    protected override string? GetEditUrl(Guid key) => $"{editUrlPrefix}/{key}";

    /// <summary>A stored version, for products that keep version history.</summary>
    protected EntityDocument? LoadVersion(Guid key, long version)
        => Database.GetVersion(EntityType, key, version) is { } row ? Document(key, row) : null;

    private EntityDocument Document(Guid key, InMemoryDatabase.Row row)
        => new() { EntityType = EntityType, Key = key, LiveVersion = row.Version, Root = EntityJson.FromJson(row.Root) };
}

/// <summary>
/// A definition written against entity documents rather than a model class: the escape hatch for entities whose
/// structure isn't a C# class, such as an Automate workflow whose step types are other packages' abilities.
/// </summary>
public abstract class InMemoryEntityDefinition(InMemoryDatabase database, string entityType, string nameField, string editUrlPrefix)
    : IEntityDefinition
{
    protected InMemoryDatabase Database { get; } = database;

    public string EntityType { get; } = entityType;

    public virtual bool CanCreate => true;

    public IUnitOfWorkProvider UnitOfWork => Database;

    public abstract Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<EntitySubtype>> GetSubtypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntitySubtype>>([]);

    public EntityDocument New(Guid key, string? subtype) => new() { EntityType = EntityType, Key = key };

    public Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(Database.Get(EntityType, key) is { } row
            ? new EntityDocument { EntityType = EntityType, Key = key, LiveVersion = row.Version, Root = EntityJson.FromJson(row.Root) }
            : null);

    public virtual Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Problem>>([]);

    public virtual Task<EntityRef> SaveAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        Database.Put(EntityType, document.Key, EntityJson.ToJson(document.Root));
        return Task.FromResult(ToRef(document));
    }

    public Task<IReadOnlyList<EntityRef>> FindAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntityRef>>(Database.All(EntityType)
            .Select(r => ToRef(new EntityDocument { EntityType = EntityType, Key = r.Key, Root = EntityJson.FromJson(r.Row.Root) }))
            .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList());

    public EntityRef ToRef(EntityDocument document)
        => new(EntityType, document.Key, document.Root.ReadAny(nameField)?.GetValue<string>() ?? "(unnamed)", $"{editUrlPrefix}/{document.Key}");
}
